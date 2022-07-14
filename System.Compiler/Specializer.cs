// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information. 

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
#if FxCop
using InterfaceList = Microsoft.Cci.InterfaceCollection;
using MemberList = Microsoft.Cci.MemberCollection;
using MethodList = Microsoft.Cci.MethodCollection;
using TypeNodeList = Microsoft.Cci.TypeNodeCollection;
using Module = Microsoft.Cci.ModuleNode;
using Class = Microsoft.Cci.ClassNode;
using Interface = Microsoft.Cci.InterfaceNode;
#endif

#if CCINamespace
namespace Microsoft.Cci{
#else
namespace System.Compiler
{
#endif
    /* Specializer walks an IR, replacing references to type parameters with references to actual types.
     * The main complication is that structural types involving type parameters need to be reconstructed.
     * Other complications arise from the fact that IL is not orthogonal and requires different instructions
     * to be used depending on whether a type is a reference type or a value type. In templates, type parameters
     * are treated as reference types when method bodies are generated. In order to instantiate a template with
     * a value type argument, it is necessary to walk the method bodies and transform some expressions. This is
     * not possible to do in a single pass because method bodies can contain references to signatures defined
     * in parts of the IR that have not yet been visited and specialized. Consequently, Specializer ignores
     * method bodies.
     * 
     * Once all signatures have been fixed up by Specializer, it is necessary to use MethodBodySpecializer
     * to walk the method bodies and fix up the IL to deal with value types that replaced type parameters.
     * Another complication to deal with is that MemberBindings and NameBindings can refer to members
     * defined in structural types based on type parameters. These must be substituted with references to the
     * corresponding members of structural types based on the type arguments. Note that some structural types
     * are themselves implemented as templates.
     */

    /// <summary>
    ///     This class specializes a normalized IR by replacing type parameters with type arguments.
    /// </summary>
#if !FxCop
    public
#endif
        class Specializer : StandardVisitor
    {
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public readonly Block DummyBody = new Block();

        public TypeNodeList args;
        public Method CurrentMethod;
        public TypeNode CurrentType;
        private readonly TrivialHashtable forwarding = new TrivialHashtable();
        public TypeNodeList pars;
        public Module TargetModule;

        public Specializer(Module targetModule, TypeNodeList pars, TypeNodeList args)
        {
            Debug.Assert(pars != null && pars.Count > 0);
            Debug.Assert(args != null && args.Count > 0);
            this.pars = pars;
            this.args = args;
            TargetModule = targetModule;
        }

        public override DelegateNode VisitDelegateNode(DelegateNode delegateNode)
        {
            return VisitTypeNode(delegateNode) as DelegateNode;
        }

        public override Interface VisitInterfaceReference(Interface Interface)
        {
            return VisitTypeReference(Interface) as Interface;
        }

        public override Expression VisitMemberBinding(MemberBinding memberBinding)
        {
            var result = base.VisitMemberBinding(memberBinding);
            var mb = result as MemberBinding;
            if (mb != null) mb.BoundMember = VisitMemberReference(mb.BoundMember);
            return result;
        }

        public virtual Member VisitMemberReference(Member member)
        {
            if (member == null) return null;
#if false && !MinimalReader
      ParameterField pField = member as ParameterField;
      if (pField != null){
        if (pField.Parameter != null) pField.Type = pField.Parameter.Type;
        return pField;
      }
#endif
            var type = member as TypeNode;
            if (type != null) return VisitTypeReference(type);

            var method = member as Method;
            if (method != null && method.Template != null && method.TemplateArguments != null &&
                method.TemplateArguments.Count > 0)
            {
                var template = VisitMemberReference(method.Template) as Method;
                // Assertion is wrong as declaring type could be instantiated and specialized. Debug.Assert(template == method.Template);
                var needNewInstance = template != null && template != method.Template;
                var args = method.TemplateArguments.Clone();
                for (int i = 0, n = args.Count; i < n; i++)
                {
                    var arg = VisitTypeReference(args[i]);
                    if (arg != null && arg != args[i])
                    {
                        args[i] = arg;
                        needNewInstance = true;
                    }
                }

                if (needNewInstance)
                    //^ assert template != null;
                    return template.GetTemplateInstance(CurrentType, args);
                return method;
            }

            var specializedType = VisitTypeReference(member.DeclaringType);
            if (specializedType == member.DeclaringType || specializedType == null) return member;
            return GetCorrespondingMember(member, specializedType);
        }

        public static Member GetCorrespondingMember(Member /*!*/ member, TypeNode /*!*/ specializedType)
        {
            //member belongs to a structural type based on a type parameter.
            //return the corresponding member from the structural type based on the type argument.
            if (member.DeclaringType == null)
            {
                Debug.Fail("");
                return null;
            }

            var unspecializedMembers = member.DeclaringType.Members;
            var specializedMembers = specializedType.Members;
            if (unspecializedMembers == null || specializedMembers == null)
            {
                Debug.Assert(false);
                return null;
            }

            var unspecializedOffset = 0;
            var specializedOffset = 0;
            //The offsets can become > 0 when the unspecialized type and/or specialized type is imported from another assembly 
            //(and the unspecialized type is in fact a partially specialized type.)
            for (int i = 0, n = specializedMembers == null ? 0 : specializedMembers.Count; i < n; i++)
            {
                var unspecializedMember = unspecializedMembers[i - unspecializedOffset];
                var specializedMember = specializedMembers[i - specializedOffset];
#if false
        if (unspecializedMember != null && specializedMember == null && unspecializedOffset == i &&
          !(unspecializedMember is TypeParameter || unspecializedMember is ClassParameter)) {
          unspecializedOffset++; continue; //Keep current unspecialized member, skip over null specialized member
        }
        if (unspecializedMember == null && specializedMember != null && specializedOffset == i &&
          !(specializedMember is TypeParameter || specializedMember is ClassParameter)) {
          specializedOffset++; continue; //Keep current specialized member, skip over null
        }
#endif
                if (unspecializedMember == member)
                {
                    Debug.Assert(specializedMember != null);
                    return specializedMember;
                }
            }

            Debug.Assert(false);
            return null;
        }

        /// <summary>
        ///     Called in 2 circumstances
        ///     1. Specializing a method because the parent type is instantiated. In this case, the method template
        ///     parameters if any, are not yet copied and need to be copied while keeping sharing alive. The copy
        ///     is necessary when the type parameter has interface constraints or base type constraints that are
        ///     being changed due to instantiation.
        ///     2. Specializing when we instantiate a generic method itself. In this case, the template parameter list
        ///     of the method is null, so no template parameter copying is done (or necessary).
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        public override Method VisitMethod(Method method)
        {
            if (method == null) return null;
            var savedCurrentMethod = CurrentMethod;
            var savedCurrentType = CurrentType;
            CurrentMethod = method;
            CurrentType = method.DeclaringType;

            // may need to copy the generic template parameters on this method and consistently substitute them
            // so we have to do this first.
            if (TargetPlatform.UseGenerics && args != method.TemplateArguments)
            {
                method.TemplateParameters = FreshTypeParameterListIfNecessary(method.TemplateParameters);
                method.TemplateArguments = VisitTypeReferenceList(method.TemplateArguments);
            }

            method.ThisParameter = (This)VisitThis(method.ThisParameter);
            method.Attributes = VisitAttributeList(method.Attributes);
            method.ReturnAttributes = VisitAttributeList(method.ReturnAttributes);
            method.SecurityAttributes = VisitSecurityAttributeList(method.SecurityAttributes);
            method.ReturnType = VisitTypeReference(method.ReturnType);
#if !MinimalReader && !CodeContracts
      method.ImplementedTypes = this.VisitTypeReferenceList(method.ImplementedTypes);
#endif
            method.Parameters = VisitParameterList(method.Parameters);
#if ExtendedRuntime || CodeContracts
            if (method.contract != null)
            {
                method.contract = VisitMethodContract(method.contract);
            }
            else if (method.ProvideContract != null && method.ProviderHandle != null)
            {
                // delay 
                var origContractProvider = method.ProvideContract;
                method.ProvideContract = (mHandle, oHandle) =>
                {
                    origContractProvider(mHandle, oHandle);
                    var savedCurrentMethod2 = CurrentMethod;
                    var savedCurrentType2 = CurrentType;
                    CurrentMethod = mHandle;
                    CurrentType = mHandle.DeclaringType;
                    VisitMethodContract(mHandle.contract);
                    CurrentType = savedCurrentType2;
                    CurrentMethod = savedCurrentMethod2;
                };
            }
#endif
            method.ImplementedInterfaceMethods = VisitMethodList(method.ImplementedInterfaceMethods);
            CurrentMethod = savedCurrentMethod;
            CurrentType = savedCurrentType;
            return method;
        }

        /// <summary>
        ///     Assumes the list itself was cloned by the duplicator.
        /// </summary>
        private TypeNodeList FreshTypeParameterListIfNecessary(TypeNodeList typeNodeList)
        {
            if (typeNodeList == null) return null;

            for (var i = 0; i < typeNodeList.Count; i++)
            {
                var tp = typeNodeList[i];
                if (tp == null) continue;
                if ((tp.Interfaces != null && tp.Interfaces.Count > 0) || tp is ClassParameter)
                    typeNodeList[i] = FreshTypeParameterIfNecessary(tp);
            }

            return VisitTypeParameterList(typeNodeList);
        }

        private TypeNode FreshTypeParameterIfNecessary(TypeNode typeParameter)
        {
            Contract.Ensures(typeParameter == null ||
                             ((ITypeParameter)typeParameter).ParameterListIndex ==
                             ((ITypeParameter)Contract.Result<TypeNode>()).ParameterListIndex);

            if (typeParameter == null) return null;

            var cp = typeParameter as ClassParameter;
            if (cp != null)
            {
                if (cp.BaseClass == SystemTypes.Object && (cp.Interfaces == null || cp.Interfaces.Count == 0))
                    // no instantiation change possible
                    return typeParameter;
                return CopyTypeParameter(cp);
            }

            var interfaces = typeParameter.Interfaces;
            if (interfaces == null || interfaces.Count == 0) return typeParameter;
            var baseType = VisitTypeReference(interfaces[0]);
            if (baseType is Interface) return CopyTypeParameter(typeParameter);
            // turn into class parameter
            return ConvertToClassParameter(baseType, typeParameter);
        }

        private TypeNode CopyTypeParameter(TypeNode typeParameter)
        {
            var fresh = (TypeNode)typeParameter.Clone();
            fresh.Interfaces = fresh.Interfaces.Clone();
            forwarding[typeParameter.UniqueKey] = fresh;
            return fresh;
        }

        private TypeNode ConvertToClassParameter(TypeNode baseType, TypeNode /*!*/ typeParameter)
        {
            ClassParameter result;
            if (typeParameter is MethodTypeParameter)
            {
                result = new MethodClassParameter();
            }
            else if (typeParameter is TypeParameter)
            {
                result = new ClassParameter();
                result.DeclaringType = typeParameter.DeclaringType;
            }
            else
            {
                return typeParameter; //give up
            }

            result.SourceContext = typeParameter.SourceContext;
            result.TypeParameterFlags = ((ITypeParameter)typeParameter).TypeParameterFlags;
#if ExtendedRuntime
      if (typeParameter.IsUnmanaged) { result.SetIsUnmanaged(); }
      if (typeParameter.IsPointerFree) { result.SetIsPointerFree(); }
#endif
            result.ParameterListIndex = ((ITypeParameter)typeParameter).ParameterListIndex;
            result.Name = typeParameter.Name;
            result.Namespace = StandardIds.ClassParameter;
            result.BaseClass = baseType is Class ? (Class)baseType : CoreSystemTypes.Object;
            result.DeclaringMember = ((ITypeParameter)typeParameter).DeclaringMember;
            result.DeclaringModule = typeParameter.DeclaringModule;
            result.Flags = typeParameter.Flags & ~TypeFlags.Interface;
            var constraints = result.Interfaces = new InterfaceList();
            var interfaces = typeParameter.Interfaces;
            for (int i = 1, n = interfaces == null ? 0 : interfaces.Count; i < n; i++)
                //^ assert interfaces != null;
                constraints.Add(interfaces[i]);
            forwarding[typeParameter.UniqueKey] = result;
            return result;
        }

        public virtual MethodList VisitMethodList(MethodList methods)
        {
            if (methods == null) return null;
            var n = methods.Count;
            for (var i = 0; i < n; i++)
                methods[i] = (Method)VisitMemberReference(methods[i]);
            return methods;
        }

        public override TypeNode VisitTypeNode(TypeNode typeNode)
        {
            if (typeNode == null) return null;
            var savedCurrentType = CurrentType;
            if (savedCurrentType != null && savedCurrentType.TemplateArguments != null &&
                savedCurrentType.TemplateArguments.Count > 0 &&
                typeNode.Template != null && (typeNode.Template.TemplateParameters == null ||
                                              typeNode.Template.TemplateParameters.Count == 0))
                typeNode.TemplateArguments = new TypeNodeList(0);
            CurrentType = typeNode;
            if (typeNode.ProvideTypeMembers != null && /*typeNode.ProvideNestedTypes != null &&*/
                typeNode.ProviderHandle != null)
            {
                typeNode.members = null;
                typeNode.ProviderHandle = new SpecializerHandle(typeNode.ProvideNestedTypes,
                    typeNode.ProvideTypeMembers, typeNode.ProvideTypeSignature, typeNode.ProvideTypeAttributes,
                    typeNode.ProviderHandle);
                typeNode.ProvideNestedTypes = ProvideNestedTypes;
                typeNode.ProvideTypeMembers = ProvideTypeMembers;
                typeNode.ProvideTypeAttributes = ProvideTypeAttributes;
                typeNode.ProvideTypeSignature = ProvideTypeSignature;
#if !MinimalReader
                var delegateNode = typeNode as DelegateNode;
                if (delegateNode != null)
                    if (!delegateNode.IsNormalized)
                    {
                        //In the Normalized case Parameters are retrieved from the Invoke method, which means evaluating Members
                        delegateNode.Parameters = VisitParameterList(delegateNode.Parameters);
                        delegateNode.ReturnType = VisitTypeReference(delegateNode.ReturnType);
                    }
#endif
            }
            else
            {
                typeNode.Attributes = VisitAttributeList(typeNode.Attributes);
                typeNode.SecurityAttributes = VisitSecurityAttributeList(typeNode.SecurityAttributes);
                var c = typeNode as Class;
                if (c != null) c.BaseClass = (Class)VisitTypeReference(c.BaseClass);
                typeNode.Interfaces = VisitInterfaceReferenceList(typeNode.Interfaces);
                typeNode.Members = VisitMemberList(typeNode.Members);
                var delegateNode = typeNode as DelegateNode;
                if (delegateNode != null)
                {
                    delegateNode.Parameters = VisitParameterList(delegateNode.Parameters);
                    delegateNode.ReturnType = VisitTypeReference(delegateNode.ReturnType);
                }
            }

            CurrentType = savedCurrentType;
            return typeNode;
        }

        private void ProvideTypeAttributes(TypeNode typeNode, object handle)
        {
            var sHandler = (SpecializerHandle)handle;
            var savedCurrentType = CurrentType;
            CurrentType = typeNode;
            if (sHandler.TypeAttributeProvider != null) sHandler.TypeAttributeProvider(typeNode, sHandler.Handle);
            typeNode.Attributes = VisitAttributeList(typeNode.Attributes);
            typeNode.SecurityAttributes = VisitSecurityAttributeList(typeNode.SecurityAttributes);

            CurrentType = savedCurrentType;
        }

        private void ProvideTypeSignature(TypeNode typeNode, object handle)
        {
            var sHandler = (SpecializerHandle)handle;
            var savedCurrentType = CurrentType;
            CurrentType = typeNode;
            if (sHandler.TypeSignatureProvider != null) sHandler.TypeSignatureProvider(typeNode, sHandler.Handle);
            var c = typeNode as Class;
            if (c != null) c.BaseClass = (Class)VisitTypeReference(c.BaseClass);
            typeNode.Interfaces = VisitInterfaceReferenceList(typeNode.Interfaces);

            CurrentType = savedCurrentType;
        }

        private void ProvideNestedTypes(TypeNode /*!*/ typeNode, object /*!*/ handle)
        {
            var sHandler = (SpecializerHandle)handle;
            if (sHandler.NestedTypeProvider == null) return;
            var savedCurrentType = CurrentType;
            CurrentType = typeNode;
            sHandler.NestedTypeProvider(typeNode, sHandler.Handle);
            var nestedTypes = typeNode.nestedTypes;
            for (int i = 0, n = nestedTypes == null ? 0 : nestedTypes.Count; i < n; i++)
            {
                //^ assert nestedTypes != null;
                var nt = nestedTypes[i];
                if (nt == null) continue;
                VisitTypeNode(nt);
            }

            CurrentType = savedCurrentType;
        }

        private void ProvideTypeMembers(TypeNode /*!*/ typeNode, object /*!*/ handle)
        {
            var sHandler = (SpecializerHandle)handle;
            var savedCurrentType = CurrentType;
            CurrentType = typeNode;
            sHandler.TypeMemberProvider(typeNode, sHandler.Handle);
            typeNode.Members = VisitMemberList(typeNode.Members);
            var delegateNode = typeNode as DelegateNode;
            if (delegateNode != null && delegateNode.IsNormalized)
            {
                delegateNode.Parameters = VisitParameterList(delegateNode.Parameters);
                delegateNode.ReturnType = VisitTypeReference(delegateNode.ReturnType);
            }

            CurrentType = savedCurrentType;
        }

        public virtual Expression VisitTypeExpression(Expression expr)
        {
            var pars = this.pars;
            var args = this.args;
            var id = expr as Identifier;
            if (id != null)
            {
                var key = id.UniqueIdKey;
                for (int i = 0, n = pars == null ? 0 : pars.Count, m = args == null ? 0 : args.Count;
                     i < n && i < m;
                     i++)
                {
                    //^ assert pars != null && args != null;
                    var par = pars[i];
                    if (par == null || par.Name == null) continue;
                    if (par.Name.UniqueIdKey == key) return new Literal(args[i], CoreSystemTypes.Type);
                }

                return id;
            }
#if !MinimalReader && !CodeContracts
      Debug.Assert(expr is QualifiedIdentifier || expr is Literal);
#endif
            return expr;
        }

        public override TypeNode VisitTypeParameter(TypeNode typeParameter)
        {
            Contract.Ensures(typeParameter == null ||
                             ((ITypeParameter)typeParameter).ParameterListIndex ==
                             ((ITypeParameter)Contract.Result<TypeNode>()).ParameterListIndex);

            return base.VisitTypeParameter(typeParameter);
        }

        public override TypeNode VisitTypeReference(TypeNode type)
        {
            //TODO: break up this method
            if (type == null) return null;
            var pars = this.pars;
            var args = this.args;
            switch (type.NodeType)
            {
                case NodeType.ArrayType:
                    var arrType = (ArrayType)type;
                    var elemType = VisitTypeReference(arrType.ElementType);
                    if (elemType == arrType.ElementType || elemType == null) return arrType;
                    if (arrType.IsSzArray()) return elemType.GetArrayType(1);
                    return elemType.GetArrayType(arrType.Rank, arrType.Sizes, arrType.LowerBounds);
#if !MinimalReader
                case NodeType.DelegateNode:
                {
                    var ftype = type as FunctionType;
                    if (ftype == null) goto default;
                    var referringType = ftype.DeclaringType == null
                        ? CurrentType
                        : VisitTypeReference(ftype.DeclaringType);
                    return FunctionType.For(VisitTypeReference(ftype.ReturnType), VisitParameterList(ftype.Parameters),
                        referringType);
                }
#endif
                case NodeType.Pointer:
                    var pType = (Pointer)type;
                    elemType = VisitTypeReference(pType.ElementType);
                    if (elemType == pType.ElementType || elemType == null) return pType;
                    return elemType.GetPointerType();
                case NodeType.Reference:
                    var rType = (Reference)type;
                    elemType = VisitTypeReference(rType.ElementType);
                    if (elemType == rType.ElementType || elemType == null) return rType;
                    return elemType.GetReferenceType();
#if ExtendedRuntime
        case NodeType.TupleType:{
          TupleType tType = (TupleType)type;
          bool reconstruct = false;
          MemberList members = tType.Members;
          int n = members == null ? 0 : members.Count;
          FieldList fields = new FieldList(n);
          for (int i = 0; i < n; i++){
            //^ assert members != null;
            Field f = members[i] as Field;
            if (f == null) continue;
            f = (Field)f.Clone();
            fields.Add(f);
            TypeNode oft = f.Type;
            TypeNode ft = f.Type = this.VisitTypeReference(f.Type);
            if (ft != oft) reconstruct = true;
          }
          if (!reconstruct) return tType;
          TypeNode referringType =
 tType.DeclaringType == null ? this.CurrentType : this.VisitTypeReference(tType.DeclaringType);
          return TupleType.For(fields, referringType);}
        case NodeType.TypeIntersection:{
          TypeIntersection tIntersect = (TypeIntersection)type;
          TypeNode referringType =
 tIntersect.DeclaringType == null ? this.CurrentType : this.VisitTypeReference(tIntersect.DeclaringType);
          return TypeIntersection.For(this.VisitTypeReferenceList(tIntersect.Types), referringType);}
        case NodeType.TypeUnion:{
          TypeUnion tUnion = (TypeUnion)type;
          TypeNode referringType =
 tUnion.DeclaringType == null ? this.CurrentType : this.VisitTypeReference(tUnion.DeclaringType);
          TypeNodeList types = this.VisitTypeReferenceList(tUnion.Types);
          if (referringType == null || types == null) { Debug.Fail(""); return null; }
          return TypeUnion.For(types, referringType);}
#endif
#if !MinimalReader
                case NodeType.ArrayTypeExpression:
                    var aExpr = (ArrayTypeExpression)type;
                    aExpr.ElementType = VisitTypeReference(aExpr.ElementType);
                    return aExpr;
                case NodeType.BoxedTypeExpression:
                    var bExpr = (BoxedTypeExpression)type;
                    bExpr.ElementType = VisitTypeReference(bExpr.ElementType);
                    return bExpr;
                case NodeType.ClassExpression:
                {
                    var cExpr = (ClassExpression)type;
                    cExpr.Expression = VisitTypeExpression(cExpr.Expression);
                    var lit = cExpr.Expression as Literal; //Could happen if the expression is a template parameter
                    if (lit != null) return lit.Value as TypeNode;
                    cExpr.TemplateArguments = VisitTypeReferenceList(cExpr.TemplateArguments);
                    return cExpr;
                }
#endif
                case NodeType.ClassParameter:
                case NodeType.TypeParameter:
                    var key = type.UniqueKey;
                    var mappedTarget = forwarding[key] as TypeNode;
                    if (mappedTarget != null) return mappedTarget;
                    for (int i = 0, n = pars == null ? 0 : pars.Count, m = args == null ? 0 : args.Count;
                         i < n && i < m;
                         i++)
                    {
                        //^ assert pars != null && args != null;
                        var tp = pars[i];
                        if (tp == null) continue;
                        if (tp.UniqueKey == key) return args[i];
#if false
            if (tp.Name.UniqueIdKey == type.Name.UniqueIdKey && (tp is ClassParameter && type is TypeParameter)) {
              //This shouldn't really happen, but in practice it does. Hack past it.
              Debug.Assert(false);
              return args[i];
            }
#endif
                    }

                    return type;
#if ExtendedRuntime
        case NodeType.ConstrainedType:{
          ConstrainedType conType = (ConstrainedType)type;
          TypeNode referringType =
 conType.DeclaringType == null ? this.CurrentType : this.VisitTypeReference(conType.DeclaringType);
          TypeNode underlyingType = this.VisitTypeReference(conType.UnderlyingType);
          Expression constraint = this.VisitExpression(conType.Constraint);
          if (referringType == null || underlyingType == null || constraint == null) { Debug.Fail(""); return null; }
          return new ConstrainedType(underlyingType, constraint, referringType);}
#endif
#if !MinimalReader
                case NodeType.FlexArrayTypeExpression:
                    var flExpr = (FlexArrayTypeExpression)type;
                    flExpr.ElementType = VisitTypeReference(flExpr.ElementType);
                    return flExpr;
                case NodeType.FunctionTypeExpression:
                    var ftExpr = (FunctionTypeExpression)type;
                    ftExpr.Parameters = VisitParameterList(ftExpr.Parameters);
                    ftExpr.ReturnType = VisitTypeReference(ftExpr.ReturnType);
                    return ftExpr;
                case NodeType.InvariantTypeExpression:
                    var invExpr = (InvariantTypeExpression)type;
                    invExpr.ElementType = VisitTypeReference(invExpr.ElementType);
                    return invExpr;
#endif
                case NodeType.InterfaceExpression:
                    var iExpr = (InterfaceExpression)type;
                    if (iExpr.Expression == null) goto default;
                    iExpr.Expression = VisitTypeExpression(iExpr.Expression);
                    iExpr.TemplateArguments = VisitTypeReferenceList(iExpr.TemplateArguments);
                    return iExpr;
#if !MinimalReader
                case NodeType.NonEmptyStreamTypeExpression:
                    var neExpr = (NonEmptyStreamTypeExpression)type;
                    neExpr.ElementType = VisitTypeReference(neExpr.ElementType);
                    return neExpr;
                case NodeType.NonNullTypeExpression:
                    var nnExpr = (NonNullTypeExpression)type;
                    nnExpr.ElementType = VisitTypeReference(nnExpr.ElementType);
                    return nnExpr;
                case NodeType.NonNullableTypeExpression:
                    var nbExpr = (NonNullableTypeExpression)type;
                    nbExpr.ElementType = VisitTypeReference(nbExpr.ElementType);
                    return nbExpr;
                case NodeType.NullableTypeExpression:
                    var nuExpr = (NullableTypeExpression)type;
                    nuExpr.ElementType = VisitTypeReference(nuExpr.ElementType);
                    return nuExpr;
#endif
                case NodeType.OptionalModifier:
                {
                    var modType = (TypeModifier)type;
                    var modifiedType = VisitTypeReference(modType.ModifiedType);
                    var modifierType = VisitTypeReference(modType.Modifier);
                    if (modifiedType == null || modifierType == null) return type;
#if ExtendedRuntime
          if (modifierType != null && modifierType == SystemTypes.NullableType){
            if (modifiedType.IsValueType) return modifiedType;
            if (TypeNode.HasModifier(modifiedType, SystemTypes.NonNullType))
              modifiedType = TypeNode.StripModifier(modifiedType, SystemTypes.NonNullType);
            if (modifiedType.IsTemplateParameter) {
              return OptionalModifier.For(modifierType, modifiedType);
            }
            return modifiedType;
          }
          if (modifierType == SystemTypes.NonNullType) {
            if (modifiedType.IsValueType) return modifiedType;
            modifiedType = TypeNode.StripModifier(modifiedType, SystemTypes.NonNullType);
          }
          //^ assert modifiedType != null;
#endif
                    return OptionalModifier.For(modifierType, modifiedType);
                }
                case NodeType.RequiredModifier:
                {
                    var modType = (TypeModifier)type;
                    var modifiedType = VisitTypeReference(modType.ModifiedType);
                    var modifierType = VisitTypeReference(modType.Modifier);
                    if (modifiedType == null || modifierType == null)
                    {
                        Debug.Fail("");
                        return type;
                    }

                    return RequiredModifier.For(modifierType, modifiedType);
                }
#if !MinimalReader && !CodeContracts
        case NodeType.OptionalModifierTypeExpression:
          OptionalModifierTypeExpression optmodType = (OptionalModifierTypeExpression)type;
          optmodType.ModifiedType = this.VisitTypeReference(optmodType.ModifiedType);
          optmodType.Modifier = this.VisitTypeReference(optmodType.Modifier);
          return optmodType;
        case NodeType.RequiredModifierTypeExpression:
          RequiredModifierTypeExpression reqmodType = (RequiredModifierTypeExpression)type;
          reqmodType.ModifiedType = this.VisitTypeReference(reqmodType.ModifiedType);
          reqmodType.Modifier = this.VisitTypeReference(reqmodType.Modifier);
          return reqmodType;
        case NodeType.PointerTypeExpression:
          PointerTypeExpression pExpr = (PointerTypeExpression)type;
          pExpr.ElementType = this.VisitTypeReference(pExpr.ElementType);
          return pExpr;
        case NodeType.ReferenceTypeExpression:
          ReferenceTypeExpression rExpr = (ReferenceTypeExpression)type;
          rExpr.ElementType = this.VisitTypeReference(rExpr.ElementType);
          return rExpr;
        case NodeType.StreamTypeExpression:
          StreamTypeExpression sExpr = (StreamTypeExpression)type;
          sExpr.ElementType = this.VisitTypeReference(sExpr.ElementType);
          return sExpr;
        case NodeType.TupleTypeExpression:
          TupleTypeExpression tuExpr = (TupleTypeExpression)type;
          tuExpr.Domains = this.VisitFieldList(tuExpr.Domains);
          return tuExpr;
        case NodeType.TypeExpression:{
          TypeExpression tExpr = (TypeExpression)type;
          tExpr.Expression = this.VisitTypeExpression(tExpr.Expression);
          if (tExpr.Expression is Literal) return type;
          tExpr.TemplateArguments = this.VisitTypeReferenceList(tExpr.TemplateArguments);
          return tExpr;}
        case NodeType.TypeIntersectionExpression:
          TypeIntersectionExpression tiExpr = (TypeIntersectionExpression)type;
          tiExpr.Types = this.VisitTypeReferenceList(tiExpr.Types);
          return tiExpr;
        case NodeType.TypeUnionExpression:
          TypeUnionExpression tyuExpr = (TypeUnionExpression)type;
          tyuExpr.Types = this.VisitTypeReferenceList(tyuExpr.Types);
          return tyuExpr;
#endif
                default:
                    if (type.Template != null)
                    {
                        Debug.Assert(TypeNode.IsCompleteTemplate(type.Template));
                        // map consolidated arguments
                        var mustSpecializeFurther = false;
                        var targs = type.ConsolidatedTemplateArguments;
                        var numArgs = targs == null ? 0 : targs.Count;
                        if (targs != null)
                        {
                            targs = targs.Clone();
                            for (var i = 0; i < numArgs; i++)
                            {
                                var targ = targs[i];
                                targs[i] = VisitTypeReference(targ);
                                if (targ != targs[i]) mustSpecializeFurther = true;
                            }
                        }

                        if (targs == null || !mustSpecializeFurther) return type;
                        var t = type.Template.GetGenericTemplateInstance(TargetModule, targs);
                        return t;
                    }

                    return type;
#if OLD
          TypeNode declaringType = this.VisitTypeReference(type.DeclaringType);
          if (declaringType != null){
            Identifier tname = type.Name;
            if (type.Template != null && type.IsGeneric) tname = type.Template.Name;
            TypeNode nt = declaringType.GetNestedType(tname);
            if (nt != null){
              TypeNodeList arguments = type.TemplateArguments;
              type = nt;
              if (TargetPlatform.UseGenerics) {
                if (arguments != null && arguments.Count > 0 && nt.ConsolidatedTemplateParameters != null && nt.ConsolidatedTemplateParameters.Count > 0)
                  type = nt.GetTemplateInstance(this.TargetModule, this.CurrentType, declaringType, arguments);
              }
            }
          }
          if (type.Template != null && (type.ConsolidatedTemplateParameters == null || type.ConsolidatedTemplateParameters.Count == 0)){
            if (!type.IsNotFullySpecialized && !type.IsNormalized) return type;
            //Type is a template instance, but some of its arguments were themselves parameters.
            //See if any of these parameters are to be specialized by this specializer.
            bool mustSpecializeFurther = false;
            TypeNodeList targs = type.TemplateArguments;
            int numArgs = targs == null ? 0 : targs.Count;
            if (targs != null) {
              targs = targs.Clone();
              for (int i = 0; i < numArgs; i++) {
                TypeNode targ = targs[i];
                ITypeParameter tparg = targ as ITypeParameter;
                if (tparg != null) {
                  for (int j = 0, np = pars == null ? 0 : pars.Count, m =
 args == null ? 0 : args.Count; j < np && j < m; j++) {
                    //^ assert pars != null && args != null;
                    if (TargetPlatform.UseGenerics) {
                      ITypeParameter par = pars[j] as ITypeParameter;
                      if (par == null) continue;
                      if (tparg == par || (tparg.ParameterListIndex == par.ParameterListIndex && tparg.DeclaringMember == par.DeclaringMember)) {
                        targ = this.args[j]; break;
                      }
                    }
                    else {
                      if (targ == pars[j]) { targ = this.args[j]; break; }
                    }
                  }
                } else {
                  if (targ != type)
                    targ = this.VisitTypeReference(targ);
                  if (targ == type) continue;
                }
                mustSpecializeFurther |= targs[i] != targ;
                targs[i] = targ;
              }
            }
            if (targs == null || !mustSpecializeFurther) return type;
            TypeNode t = type.Template.GetTemplateInstance(this.TargetModule, this.CurrentType, declaringType, targs);
#if ExtendedRuntime
            if (this.CurrentType != null) {
              if (this.CurrentType.ReferencedTemplateInstances == null) this.CurrentType.ReferencedTemplateInstances =
 new TypeNodeList();
              this.CurrentType.ReferencedTemplateInstances.Add(t);
            }
#endif
            return t;
          }
          TypeNodeList tPars = type.TemplateParameters;
          if (tPars == null || tPars.Count == 0) return type; //Not a parameterized type. No need to get an instance.
          TypeNodeList tArgs = new TypeNodeList();
          for (int i = 0, n = tPars.Count; i < n; i++) {
            TypeNode tPar = tPars[i];
            tArgs.Add(tPar); //Leave parameter in place if there is no match
            if (tPar == null || tPar.Name == null) continue;
            int idKey = tPar.Name.UniqueIdKey;
            for (int j = 0, m = pars == null ? 0 : pars.Count, k = args == null ? 0 : args.Count; j < m && j < k; j++) {
              //^ assert pars != null && args != null;
              TypeNode par = pars[j];
              if (par == null || par.Name == null) continue;
              if (par.Name.UniqueIdKey == idKey) {
                tArgs[i] = args[j];
                break;
              }
            }
          }
          TypeNode ti =
 type.GetTemplateInstance(this.TargetModule, this.CurrentType, this.VisitTypeReference(type.DeclaringType), tArgs);
#if ExtendedRuntime
          if (this.CurrentType != null) {
            if (this.CurrentType.ReferencedTemplateInstances == null) this.CurrentType.ReferencedTemplateInstances =
 new TypeNodeList();
            this.CurrentType.ReferencedTemplateInstances.Add(ti);
          }
#endif
          return ti;
#endif
            }
        }

        internal class SpecializerHandle
        {
            internal readonly object /*!*/
                Handle;

            internal readonly TypeNode.NestedTypeProvider /*!*/
                NestedTypeProvider;

            internal readonly TypeNode.TypeAttributeProvider TypeAttributeProvider;

            internal readonly TypeNode.TypeMemberProvider /*!*/
                TypeMemberProvider;

            internal readonly TypeNode.TypeSignatureProvider TypeSignatureProvider;

            internal SpecializerHandle(TypeNode.NestedTypeProvider /*!*/ nestedTypeProvider,
                TypeNode.TypeMemberProvider /*!*/ typeMemberProvider,
                TypeNode.TypeSignatureProvider typeSignatureProvider,
                TypeNode.TypeAttributeProvider typeAttributeProvider, object /*!*/ handle)
            {
                NestedTypeProvider = nestedTypeProvider;
                TypeMemberProvider = typeMemberProvider;
                TypeSignatureProvider = typeSignatureProvider;
                TypeAttributeProvider = typeAttributeProvider;
                Handle = handle;
                //^ base();
            }
        }
#if !MinimalReader
        public Specializer(Visitor callingVisitor)
            : base(callingVisitor)
        {
        }

        public override void TransferStateTo(Visitor targetVisitor)
        {
            base.TransferStateTo(targetVisitor);
            var target = targetVisitor as Specializer;
            if (target == null) return;
            target.args = args;
            target.pars = pars;
            target.CurrentMethod = CurrentMethod;
            target.CurrentType = CurrentType;
        }
#endif

#if ExtendedRuntime || CodeContracts
        public override MethodContract VisitMethodContract(MethodContract contract)
        {
            if (contract == null) return null;
            var specializer = this as MethodBodySpecializer;
            if (specializer == null)
            {
                specializer =
                    contract.DeclaringMethod.DeclaringType.DeclaringModule.GetMethodBodySpecializer(pars, args);
                specializer.CurrentMethod = CurrentMethod;
                specializer.CurrentType = CurrentType;
            }

            contract.contractInitializer = specializer.VisitBlock(contract.contractInitializer);
            contract.postPreamble = specializer.VisitBlock(contract.postPreamble);
            contract.ensures = specializer.VisitEnsuresList(contract.ensures);
            contract.asyncEnsures = specializer.VisitEnsuresList(contract.asyncEnsures);
            contract.modelEnsures = specializer.VisitEnsuresList(contract.modelEnsures);
            contract.modifies = specializer.VisitExpressionList(contract.modifies);
            contract.requires = specializer.VisitRequiresList(contract.requires);
            return contract;
        }

        public virtual object VisitContractPart(Method method, object part)
        {
            if (method == null)
            {
                Debug.Fail("method == null");
                return part;
            }

            CurrentMethod = method;
            CurrentType = method.DeclaringType;
            var es = part as EnsuresList;
            if (es != null) return VisitEnsuresList(es);
            var rs = part as RequiresList;
            if (rs != null) return VisitRequiresList(rs);
            var initializer = part as Block;
            if (initializer != null) return VisitBlock(initializer);
            return part;
        }
#endif
    }
#if !NoWriter
    public class MethodBodySpecializer : Specializer
    {
        public TrivialHashtable /*!*/
            alreadyVisitedNodes = new TrivialHashtable();

        public Method methodBeingSpecialized;
        public Method dummyMethod;

        public MethodBodySpecializer(Module module, TypeNodeList pars, TypeNodeList args)
            : base(module, pars, args)
        {
            //^ base;
        }
#if !MinimalReader
        public MethodBodySpecializer(Visitor callingVisitor)
            : base(callingVisitor)
        {
            //^ base;
        }
#endif
        public override Node Visit(Node node)
        {
            var lit = node as Literal;
            if (lit != null && lit.Value == null) return lit;
            var e = node as Expression;
            if (e != null && !(e is Local || e is Parameter))
                e.Type = VisitTypeReference(e.Type);
            return base.Visit(node);
        }

        public override Expression VisitAddressDereference(AddressDereference addr)
        {
            if (addr == null) return null;
            var unboxDeref = addr.Address != null && addr.Address.NodeType == NodeType.Unbox;
            addr.Address = VisitExpression(addr.Address);
            if (addr.Address == null) return null;
            if (unboxDeref && addr.Address.NodeType != NodeType.Unbox) return addr.Address;
            var reference = addr.Address.Type as Reference;
            if (reference != null) addr.Type = reference.ElementType;
            return addr;
        }

        public override Statement VisitAssignmentStatement(AssignmentStatement assignment)
        {
            assignment = (AssignmentStatement)base.VisitAssignmentStatement(assignment);
            if (assignment == null) return null;
            var target = assignment.Target;
            var source = assignment.Source;
            var tType = target == null ? null : target.Type;
            var sType = source == null ? null : source.Type;
            if (tType != null && sType != null)
            {
                //^ assert target != null;
                if (tType.IsValueType)
                {
                    if (sType is Reference)
                        assignment.Source = new AddressDereference(source, tType);
                    else if (!sType.IsValueType && !(sType == CoreSystemTypes.Object && source is Literal &&
                                                     target.NodeType == NodeType.AddressDereference))
                        assignment.Source = new AddressDereference(
                            new BinaryExpression(source, new MemberBinding(null, sType), NodeType.Unbox), sType);
                }
                else
                {
                    if (sType.IsValueType)
                        if (!(tType is Reference))
                            assignment.Source = new BinaryExpression(source, new MemberBinding(null, sType),
                                NodeType.Box, tType);
                }
            }

            return assignment;
        }

        public override Expression VisitBinaryExpression(BinaryExpression binaryExpression)
        {
            if (binaryExpression == null) return null;
            var opnd1IsInst = binaryExpression.Operand1 != null &&
                              binaryExpression.Operand1.NodeType == NodeType.Isinst;
            binaryExpression = (BinaryExpression)base.VisitBinaryExpression(binaryExpression);
            if (binaryExpression == null) return null;
            var opnd1 = binaryExpression.Operand1;
            var opnd2 = binaryExpression.Operand2;
            var lit = opnd2 as Literal;
            var t = lit == null ? null : lit.Value as TypeNode;
            if (binaryExpression.NodeType ==
                NodeType.Castclass /*|| binaryExpression.NodeType == NodeType.ExplicitCoercion*/)
            {
                //See if castclass must become box or unbox
                if (t != null)
                {
                    if (t.IsValueType)
                    {
                        var adref = new AddressDereference(new BinaryExpression(opnd1, lit, NodeType.Unbox), t);
                        adref.Type = t;
                        return adref;
                    }

                    if (opnd1 != null && opnd1.Type != null && opnd1.Type.IsValueType)
                        return new BinaryExpression(opnd1, new MemberBinding(null, opnd1.Type), NodeType.Box, t);
                }
            }
            else if (binaryExpression.NodeType == NodeType.Unbox)
            {
                if (opnd1 != null && opnd1.Type != null && opnd1.Type.IsValueType)
                    return opnd1;
#if ExtendedRuntime
      }else if (binaryExpression.NodeType == NodeType.Box){
        if (t != null && !(t is ITypeParameter) && t.IsReferenceType && !t.IsPointerType) { // using pointer types is a Sing# extension
          return opnd1;
        }
#endif
            }
            else if (binaryExpression.NodeType == NodeType.Eq)
            {
                //For value types, turn comparisons against null into false
                if (lit != null && lit.Value == null && opnd1 != null && opnd1.Type != null && opnd1.Type.IsValueType)
                    return Literal.False;
                lit = opnd1 as Literal;
                if (lit != null && lit.Value == null && opnd2 != null && opnd2.Type != null && opnd2.Type.IsValueType)
                    return Literal.False;
            }
            else if (binaryExpression.NodeType == NodeType.Ne)
            {
                //For value types, turn comparisons against null into true
                if (lit != null && lit.Value == null && opnd1 != null && opnd1.Type != null && opnd1.Type.IsValueType)
                {
                    if (opnd1IsInst && opnd1.Type == CoreSystemTypes.Boolean) return opnd1;
                    return Literal.True;
                }

                lit = opnd1 as Literal;
                if (lit != null && lit.Value == null && opnd2 != null && opnd2.Type != null && opnd2.Type.IsValueType)
                    return Literal.True;
            }
            else if (binaryExpression.NodeType == NodeType.Isinst)
            {
                //Do not emit isinst instruction if opnd1 is a value type.
                if (opnd1 != null && opnd1.Type != null && opnd1.Type.IsValueType)
                {
                    if (opnd1.Type == t)
                        return Literal.True;
                    return Literal.False;
                }
            }

            return binaryExpression;
        }

        public override Statement VisitBranch(Branch branch)
        {
            branch = (Branch)base.VisitBranch(branch);
            if (branch == null) return null;
            if (branch.Condition != null && !(branch.Condition is BinaryExpression))
            {
                //Deal with implicit comparisons against null
                var ct = branch.Condition.Type;
                if (ct != null && !ct.IsPrimitiveInteger && ct != CoreSystemTypes.Boolean && ct.IsValueType)
                {
                    if (branch.Condition.NodeType == NodeType.LogicalNot)
                        return null;
                    branch.Condition = null;
                }
            }

            return branch;
        }

        public override Expression VisitExpression(Expression expression)
        {
            if (expression == null) return null;
            switch (expression.NodeType)
            {
                case NodeType.Dup:
                case NodeType.Arglist:
                    expression.Type = VisitTypeReference(expression.Type);
                    return expression;
                case NodeType.Pop:
                    expression.Type = VisitTypeReference(expression.Type);
                    var uex = expression as UnaryExpression;
                    if (uex != null)
                    {
                        uex.Operand = VisitExpression(uex.Operand);
                        return uex;
                    }

                    return expression;
                default:
                    return (Expression)Visit(expression);
            }
        }

        public override Expression VisitIndexer(Indexer indexer)
        {
            indexer = (Indexer)base.VisitIndexer(indexer);
            if (indexer == null || indexer.Object == null) return null;
            var arrType = indexer.Object.Type as ArrayType;
            if (arrType != null) indexer.Type = indexer.ElementType = arrType.ElementType;
            else
                indexer.ElementType = VisitTypeReference(indexer.ElementType);

            //if (elemType != null && elemType.IsValueType && !elemType.IsPrimitive)
            //return new AddressDereference(new UnaryExpression(indexer, NodeType.AddressOf), elemType);
            return indexer;
        }

        public override Expression VisitLiteral(Literal literal)
        {
            if (literal == null) return null;
            var t = literal.Value as TypeNode;
            if (t != null && literal.Type == CoreSystemTypes.Type)
                return new Literal(VisitTypeReference(t), literal.Type, literal.SourceContext);
            return (Literal)literal.Clone();
        }

        public override Expression VisitLocal(Local local)
        {
            if (local == null) return null;
            if (alreadyVisitedNodes[local.UniqueKey] != null) return local;
            alreadyVisitedNodes[local.UniqueKey] = local;
            return base.VisitLocal(local);
        }
#if !MinimalReader && !CodeContracts
    public override Statement VisitLocalDeclarationsStatement(LocalDeclarationsStatement localDeclarations) {
      if (localDeclarations == null) return null;
      localDeclarations.Type = this.VisitTypeReference(localDeclarations.Type);
      return localDeclarations;
    }
#endif
        public override Expression VisitParameter(Parameter parameter)
        {
#if !MinimalReader
            var pb = parameter as ParameterBinding;
            if (pb != null && pb.BoundParameter != null)
                pb.Type = pb.BoundParameter.Type;
#endif
            return parameter;
        }
#if !MinimalReader && !CodeContracts
    public override Expression VisitNameBinding(NameBinding nameBinding){
      if (nameBinding == null) return null;
      nameBinding.BoundMember = this.VisitExpression(nameBinding.BoundMember);
      int n = nameBinding.BoundMembers == null ? 0 : nameBinding.BoundMembers.Count;
      for (int i = 0; i < n; i++) {
        //^ assert nameBinding.BoundMembers != null;
        nameBinding.BoundMembers[i] = this.VisitMemberReference(nameBinding.BoundMembers[i]);
      }
      return nameBinding;
    }
#endif
        public override Expression VisitMemberBinding(MemberBinding memberBinding)
        {
            if (memberBinding == null) return null;
            var tObj = memberBinding.TargetObject = VisitExpression(memberBinding.TargetObject);
            var mem = VisitMemberReference(memberBinding.BoundMember);
            if (mem == dummyMethod)
                mem = methodBeingSpecialized;
            Debug.Assert(mem != null);
            memberBinding.BoundMember = mem;
            if (memberBinding == null) return null;
            var method = memberBinding.BoundMember as Method;
            if (method != null) //Need to take the address of the target object (this parameter), or need to box it, if this target object type is value type
                if (tObj != null && tObj.Type != null && tObj.Type.IsValueType)
                {
                    if (tObj.NodeType != NodeType.This)
                    {
                        if (method.DeclaringType != null &&
                            method.DeclaringType.IsValueType) //it expects the address of the value type
                        {
                            memberBinding.TargetObject = new UnaryExpression(memberBinding.TargetObject,
                                NodeType.AddressOf, memberBinding.TargetObject.Type.GetReferenceType());
                        }
                        else
                        {
                            //it expects a boxed copy of the value type
                            var obType = new MemberBinding(null, memberBinding.TargetObject.Type);
                            memberBinding.TargetObject = new BinaryExpression(memberBinding.TargetObject, obType,
                                NodeType.Box, method.DeclaringType);
                        }
                    }
                }

            var t = memberBinding.BoundMember as TypeNode;
            if (t != null)
            {
                var t1 = VisitTypeReference(t);
                memberBinding.BoundMember = t1;
            }

            return memberBinding;
        }

        public override Method VisitMethod(Method method)
        {
            if (method == null) return null;
            var savedCurrentMethod = CurrentMethod;
            var savedCurrentType = CurrentType;
            CurrentMethod = method;
            CurrentType = method.DeclaringType;
            method.Body = VisitBlock(method.Body);
            CurrentMethod = savedCurrentMethod;
            CurrentType = savedCurrentType;
            return method;
        }

        public override Expression VisitConstruct(Construct cons)
        {
            cons = (Construct)base.VisitConstruct(cons);
            if (cons == null) return null;
            var mb = cons.Constructor as MemberBinding;
            if (mb == null) return cons;
            var meth = mb.BoundMember as Method;
            if (meth == null) return cons;
            var parameters = meth.Parameters;
            if (parameters == null) return cons;
            var operands = cons.Operands;
            var n = operands == null ? 0 : operands.Count;
            if (n > parameters.Count) n = parameters.Count;
            for (var i = 0; i < n; i++)
            {
                //^ assert operands != null;
                var e = operands[i];
                if (e == null) continue;
                var p = parameters[i];
                if (p == null) continue;
                if (e.Type == null || p.Type == null) continue;
                if (e.Type.IsValueType && !p.Type.IsValueType)
                    operands[i] = new BinaryExpression(e, new MemberBinding(null, e.Type), NodeType.Box, p.Type);
            }

            return cons;
        }

        public override Expression VisitMethodCall(MethodCall call)
        {
            call = (MethodCall)base.VisitMethodCall(call);
            if (call == null) return null;
            var mb = call.Callee as MemberBinding;
            if (mb == null) return call;
            var meth = mb.BoundMember as Method;
            if (meth == null) return call;
            var parameters = meth.Parameters;
            if (parameters == null) return call;
            var operands = call.Operands;
            var n = operands == null ? 0 : operands.Count;
            if (n > parameters.Count) n = parameters.Count;
            for (var i = 0; i < n; i++)
            {
                //^ assert operands != null;
                var e = operands[i];
                if (e == null) continue;
                var p = parameters[i];
                if (p == null) continue;
                if (e.Type == null || p.Type == null) continue;
                if (e.Type.IsValueType && !p.Type.IsValueType)
                    operands[i] = new BinaryExpression(e, new MemberBinding(null, e.Type), NodeType.Box, p.Type);
            }

            if (meth.ReturnType != null && call.Type != null && meth.ReturnType.IsValueType && !call.Type.IsValueType)
                return new BinaryExpression(call, new MemberBinding(null, meth.ReturnType), NodeType.Box, call.Type);
            return call;
        }

        public override Statement VisitReturn(Return Return)
        {
            Return = (Return)base.VisitReturn(Return);
            if (Return == null) return null;
            var rval = Return.Expression;
            if (rval == null || rval.Type == null || CurrentMethod == null || CurrentMethod.ReturnType == null)
                return Return;
            if (rval.Type.IsValueType && !CurrentMethod.ReturnType.IsValueType)
                Return.Expression = new BinaryExpression(rval, new MemberBinding(null, rval.Type), NodeType.Box,
                    CurrentMethod.ReturnType);
            return Return;
        }

        public override TypeNode VisitTypeNode(TypeNode typeNode)
        {
            if (typeNode == null) return null;
            var savedCurrentType = CurrentType;
            CurrentType = typeNode;
            var members = typeNode.Members;
            for (int i = 0, n = members == null ? 0 : members.Count; i < n; i++)
            {
                //^ assert members != null;
                var mem = members[i];
                var t = mem as TypeNode;
                if (t != null)
                {
                    VisitTypeNode(t);
                    continue;
                }

                var m = mem as Method;
                if (m != null)
                {
                    VisitMethod(m);
                }
            }

            CurrentType = savedCurrentType;
            return typeNode;
        }

        public override Expression VisitUnaryExpression(UnaryExpression unaryExpression)
        {
            if (unaryExpression == null) return null;
            return base.VisitUnaryExpression((UnaryExpression)unaryExpression.Clone());
        }
    }
#endif
}