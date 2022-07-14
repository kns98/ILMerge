// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information. 

using System.Diagnostics.CodeAnalysis;

#if CCINamespace
namespace Microsoft.Cci{
#else
namespace System.Compiler
{
#endif
#if !FxCop
    public
#endif
        class StandardIds
    {
        private StandardIds()
        {
        }

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            Address = Identifier.For("Address");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            AllowMultiple = Identifier.For("AllowMultiple");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            ClassParameter = Identifier.For("class parameter");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            Ctor = Identifier.For(".ctor");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            CCtor = Identifier.For(".cctor");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            Enum = Identifier.For("Enum");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            Get = Identifier.For("Get");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            Inherited = Identifier.For("Inherited");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            Invoke = Identifier.For("Invoke");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            Set = Identifier.For("Set");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            System = Identifier.For("System");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            This = Identifier.For("this");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            TypeParameter = Identifier.For("type parameter");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            Value__ = Identifier.For("value__");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            _Deleted = Identifier.For("_Deleted");
#if !NoXml || !MinimalReader
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            opExplicit = Identifier.For("op_Explicit");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            opImplicit = Identifier.For("op_Implicit");
#endif
#if !MinimalReader
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            Add = Identifier.For("Add");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            AllowMultipleAttribute = Identifier.For("AllowMultipleAttribute");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            Anonymity = Identifier.For("Anonymity");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            ArgumentOutOfRangeException = Identifier.For("ArgumentOutOfRangeException");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            Assembly = Identifier.For("Assembly");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            Assert = Identifier.For("Assert");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            BeginInvoke = Identifier.For("BeginInvoke");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            callback = Identifier.For("callback");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            CallingConvention = Identifier.For("CallingConvention");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            CapitalObject = Identifier.For("Object");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            CharSet = Identifier.For("CharSet");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            Class = Identifier.For("Class");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            Clear = Identifier.For("Clear");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            Closure = Identifier.For("Closure");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            Collection = Identifier.For("Collection");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            Combine = Identifier.For("Combine");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            Concat = Identifier.For("Concat");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            Count = Identifier.For("Count");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            CreateInstance = Identifier.For("CreateInstance");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            CultureName = Identifier.For("CultureName");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            Current = Identifier.For("Current");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            Dispose = Identifier.For("Dispose");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            ElementType = Identifier.For("ElementType");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            Enter = Identifier.For("Enter");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            EntryPoint = Identifier.For("EntryPoint");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            ExactSpelling = Identifier.For("ExactSpelling");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            Exit = Identifier.For("Exit");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            EndInvoke = Identifier.For("EndInvoke");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public new static readonly Identifier /*!*/
            Equals = Identifier.For("Equals");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            Finalize = Identifier.For("Finalize");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            FromObject = Identifier.For("FromObject");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            getCurrent = Identifier.For("get_Current");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            getCount = Identifier.For("get_Count");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            GetEnumerator = Identifier.For("GetEnumerator");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public new static readonly Identifier /*!*/
            GetHashCode = Identifier.For("GetHashCode");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            getHasValue = Identifier.For("get_HasValue");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            getItem = Identifier.For("get_Item");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            GetTag = Identifier.For("GetTag");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            GetTagAsType = Identifier.For("GetTagAsType");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            getValue = Identifier.For("get_Value");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            GetValue = Identifier.For("GetValue");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            GetValueOrDefault = Identifier.For("GetValueOrDefault");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public new static readonly Identifier /*!*/
            GetType = Identifier.For("GetType");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            Global = Identifier.For("global");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            HResult = Identifier.For("HResult");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            IBindableIterable = Identifier.For("IBindableIterable");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            IBindableVector = Identifier.For("IBindableVector");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            IClosable = Identifier.For("IClosable");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            ICommand = Identifier.For("ICommand");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            IFactory = Identifier.For("IFactory");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            IEnumerableGetEnumerator = Identifier.For("IEnumerable.GetEnumerator");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            IEnumeratorGetCurrent = Identifier.For("IEnumerator.get_Current");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            IEnumeratorReset = Identifier.For("IEnumerator.Reset");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            IIterable1 = Identifier.For("IIterable`1");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            IKeyValuePair2 = Identifier.For("IKeyValuePair`2");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            IMap2 = Identifier.For("IMap`2");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            IMapView2 = Identifier.For("IMapView`2");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            INotifyCollectionChanged = Identifier.For("INotifyCollectionChanged");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            IndexOf = Identifier.For("IndexOf");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            Insert = Identifier.For("Insert");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            IsInterned = Identifier.For("IsInterned");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            IsNull = Identifier.For("IsNull");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            IReference1 = Identifier.For("IReference`1");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            It = Identifier.For("it");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            Item = Identifier.For("Item");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            IVector1 = Identifier.For("IVector`1");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            IVectorView1 = Identifier.For("IVectorView`1");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            Length = Identifier.For("Length");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            Main = Identifier.For("Main");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            Method = Identifier.For("method");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public new static readonly Identifier /*!*/
            MemberwiseClone = Identifier.For("MemberwiseClone");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            MoveNext = Identifier.For("MoveNext");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            Namespace = Identifier.For("Namespace");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            New = Identifier.For("New");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            NewObj = Identifier.For(".newObj"); // used for locals representing new C() for value types

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            NotifyCollectionChangedAction = Identifier.For("NotifyCollectionChangedAction");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            NotifyCollectionChangedEventArgs = Identifier.For("NotifyCollectionChangedEventArgs");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            NotifyCollectionChangedEventHandler = Identifier.For("NotifyCollectionChangedEventHandler");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            Object = Identifier.For("object");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            opAddition = Identifier.For("op_Addition");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            opBitwiseAnd = Identifier.For("op_BitwiseAnd");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            opBitwiseOr = Identifier.For("op_BitwiseOr");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            opComma = Identifier.For("op_Comma");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            opDecrement = Identifier.For("op_Decrement");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            opDivision = Identifier.For("op_Division");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            opEquality = Identifier.For("op_Equality");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            opExclusiveOr = Identifier.For("op_ExclusiveOr");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            opFalse = Identifier.For("op_False");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            opGreaterThan = Identifier.For("op_GreaterThan");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            opGreaterThanOrEqual = Identifier.For("op_GreaterThanOrEqual");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            opIncrement = Identifier.For("op_Increment");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            opInequality = Identifier.For("op_Inequality");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            opLeftShift = Identifier.For("op_LeftShift");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            opLessThan = Identifier.For("op_LessThan");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            opLessThanOrEqual = Identifier.For("op_LessThanOrEqual");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            opLogicalNot = Identifier.For("op_LogicalNot");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            opModulus = Identifier.For("op_Modulus");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            opMultiply = Identifier.For("op_Multiply");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            opOnesComplement = Identifier.For("op_OnesComplement");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            opRightShift = Identifier.For("op_RightShift");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            opSubtraction = Identifier.For("op_Subtraction");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            opTrue = Identifier.For("op_True");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            opUnaryNegation = Identifier.For("op_UnaryNegation");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            opUnaryPlus = Identifier.For("op_UnaryPlus");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            Pack = Identifier.For("Pack");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            Phase = Identifier.For("Phase");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            Position = Identifier.For("Position");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            PreserveSig = Identifier.For("PreserveSig");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public new static readonly Identifier /*!*/
            ReferenceEquals = Identifier.For("ReferenceEquals");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            Remove = Identifier.For("Remove");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            Replace = Identifier.For("Replace");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            Reset = Identifier.For("Reset");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            result = Identifier.For("result");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            SetLastError = Identifier.For("SetLastError");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            SetValue = Identifier.For("SetValue");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            Size = Identifier.For("Size");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            StructuralTypes = Identifier.For("StructuralTypes");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            Tag = Identifier.For("tag");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            TagType = Identifier.For("tagType");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            ThisValue = Identifier.For("this value: ");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            ToObject = Identifier.For("ToObject");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public new static readonly Identifier /*!*/
            ToString = Identifier.For("ToString");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            TypeName = Identifier.For("TypeName");
#if CCINamespace
    [Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]public static readonly Identifier/*!*/ CciTypeExtensions
 = Identifier.For("Microsoft.Cci.TypeExtensions");
#else
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            CciTypeExtensions = Identifier.For("System.Compiler.TypeExtensions");
#endif
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            Value = Identifier.For("value");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            Var = Identifier.For("var");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            WindowsFoundation = Identifier.For("Windows.Foundation");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            WindowsFoundationMetadata = Identifier.For("Windows.Foundation.Metadata");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            WindowsFoundationCollections = Identifier.For("Windows.Foundation.Collections");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            WindowsUI = Identifier.For("Windows.UI");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            WindowsUIXaml = Identifier.For("Windows.UI.Xaml");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            WindowsUIXamlData = Identifier.For("Windows.UI.Xaml.Data");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            WindowsUIXamlInput = Identifier.For("Windows.UI.Xaml.Input");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            WindowsUIXamlInterop = Identifier.For("Windows.UI.Xaml.Interop");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            WindowsUIXamlControlsPrimitives = Identifier.For("Windows.UI.Xaml.Controls.Primitives");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            WindowsUIXamlMedia = Identifier.For("Windows.UI.Xaml.Media");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            WindowsUIXamlMediaAnimation = Identifier.For("Windows.UI.Xaml.Media.Animation");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            WindowsUIXamlMediaMedia3D = Identifier.For("Windows.UI.Xaml.Media.Media3D");

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            __Arglist = Identifier.For("__arglist");
#endif
    }
}