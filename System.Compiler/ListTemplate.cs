// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information. 

#if !FxCop
#if CLOUSOT
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
#endif

#if CCINamespace
namespace Microsoft.Cci{
#else
namespace System.Compiler
{
#endif
#if !MinimalReader && !CodeContracts
  public sealed class AliasDefinitionList{
    private AliasDefinition[]/*!*/ elements;
    private int count = 0;
    public AliasDefinitionList(){
      this.elements = new AliasDefinition[4];
      //^ base();
    }
    public AliasDefinitionList(int capacity){
      this.elements = new AliasDefinition[capacity];
      //^ base();
    }
    public void Add(AliasDefinition element){
      int n = this.elements.Length;
      int i = this.count++;
      if (i == n){
        int m = n*2; if (m < 4) m = 4;
        AliasDefinition[] newElements = new AliasDefinition[m];
        for (int j = 0; j < n; j++) newElements[j] = elements[j];
        this.elements = newElements;
      }
      this.elements[i] = element;
    }
    public AliasDefinitionList/*!*/ Clone() {
      AliasDefinition[] elements = this.elements;
      int n = this.count;
      AliasDefinitionList result = new AliasDefinitionList(n);
      result.count = n;
      AliasDefinition[] newElements = result.elements;
      for (int i = 0; i < n; i++)
        newElements[i] = elements[i];
      return result;
    }
    public int Count {
      get{return this.count;}
    }
    [Obsolete("Use Count property instead.")]
    public int Length{
      get{return this.count;}
    }
    public AliasDefinition this[int index]{
      get{
        return this.elements[index];
      }
      set{
        this.elements[index] = value;
      }
    }
    public Enumerator GetEnumerator(){
      return new Enumerator(this);
    }
    public struct Enumerator{
      private int index;
      private readonly AliasDefinitionList/*!*/ list;
      public Enumerator(AliasDefinitionList/*!*/ list) {
        this.index = -1;
        this.list = list;
      }
      public AliasDefinition Current{
        get{
          return this.list[this.index];
        }
      }
      public bool MoveNext(){
        return ++this.index < this.list.count;
      }
      public void Reset(){
        this.index = -1;
      }
    }
  }
#endif
    public sealed class AssemblyNodeList
    {
        private AssemblyNode[] /*!*/
            elements;

        public AssemblyNodeList()
        {
            elements = new AssemblyNode[4];
            //^ base();
        }

        public AssemblyNodeList(int capacity)
        {
            elements = new AssemblyNode[capacity];
            //^ base();
        }

        public AssemblyNodeList(params AssemblyNode[] elements)
        {
            if (elements == null) elements = new AssemblyNode[0];
            this.elements = elements;
            Count = elements.Length;
            //^ base();
        }

        public int Count { get; private set; }

        [Obsolete("Use Count property instead.")]
        public int Length => Count;

        public AssemblyNode this[int index]
        {
            get { return elements[index]; }
            set { elements[index] = value; }
        }

        public void Add(AssemblyNode element)
        {
            var n = elements.Length;
            var i = Count++;
            if (i == n)
            {
                var m = n * 2;
                if (m < 4) m = 4;
                var newElements = new AssemblyNode[m];
                for (var j = 0; j < n; j++) newElements[j] = elements[j];
                elements = newElements;
            }

            elements[i] = element;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        public struct Enumerator
        {
            private int index;

            private readonly AssemblyNodeList /*!*/
                list;

            public Enumerator(AssemblyNodeList /*!*/ list)
            {
                index = -1;
                this.list = list;
            }

            public AssemblyNode Current => list[index];

            public bool MoveNext()
            {
                return ++index < list.Count;
            }

            public void Reset()
            {
                index = -1;
            }
        }
    }

    public sealed class AssemblyReferenceList
    {
        private AssemblyReference[] /*!*/
            elements;

        public AssemblyReferenceList()
        {
            elements = new AssemblyReference[4];
            //^ base();
        }

        public AssemblyReferenceList(int capacity)
        {
            elements = new AssemblyReference[capacity];
            //^ base();
        }

        public int Count { get; private set; }

        [Obsolete("Use Count property instead.")]
        public int Length => Count;

        public AssemblyReference this[int index]
        {
            get { return elements[index]; }
            set { elements[index] = value; }
        }

        public void Add(AssemblyReference element)
        {
            var n = elements.Length;
            var i = Count++;
            if (i == n)
            {
                var m = n * 2;
                if (m < 4) m = 4;
                var newElements = new AssemblyReference[m];
                for (var j = 0; j < n; j++) newElements[j] = elements[j];
                elements = newElements;
            }

            elements[i] = element;
        }

        public AssemblyReferenceList /*!*/ Clone()
        {
            var elements = this.elements;
            var n = Count;
            var result = new AssemblyReferenceList(n);
            result.Count = n;
            var newElements = result.elements;
            for (var i = 0; i < n; i++)
                newElements[i] = elements[i];
            return result;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        public struct Enumerator
        {
            private int index;

            private readonly AssemblyReferenceList /*!*/
                list;

            public Enumerator(AssemblyReferenceList /*!*/ list)
            {
                index = -1;
                this.list = list;
            }

            public AssemblyReference Current => list[index];

            public bool MoveNext()
            {
                return ++index < list.Count;
            }

            public void Reset()
            {
                index = -1;
            }
        }
    }

    public sealed class AttributeList
    {
        private AttributeNode[] /*!*/
            elements;

        public AttributeList()
        {
            elements = new AttributeNode[4];
            //^ base();
        }

        public AttributeList(int capacity)
        {
            elements = new AttributeNode[capacity];
            //^ base();
        }

        public AttributeList(params AttributeNode[] elements)
        {
            if (elements == null) elements = new AttributeNode[0];
            this.elements = elements;
            Count = elements.Length;
            //^ base();
        }

        public int Count { get; private set; }

        [Obsolete("Use Count property instead.")]
        public int Length => Count;

        public AttributeNode this[int index]
        {
            get { return elements[index]; }
            set { elements[index] = value; }
        }

        public void Add(AttributeNode element)
        {
            var n = elements.Length;
            var i = Count++;
            if (i == n)
            {
                var m = n * 2;
                if (m < 8) m = 8;
                var newElements = new AttributeNode[m];
                for (var j = 0; j < n; j++) newElements[j] = elements[j];
                elements = newElements;
            }

            elements[i] = element;
        }

        public AttributeList /*!*/ Clone()
        {
            var elements = this.elements;
            var n = Count;
            var result = new AttributeList(n);
            result.Count = n;
            var newElements = result.elements;
            for (var i = 0; i < n; i++)
                newElements[i] = elements[i];
            return result;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        public struct Enumerator
        {
            private int index;

            private readonly AttributeList /*!*/
                list;

            public Enumerator(AttributeList /*!*/ list)
            {
                index = -1;
                this.list = list;
            }

            public AttributeNode Current => list[index];

            public bool MoveNext()
            {
                return ++index < list.Count;
            }

            public void Reset()
            {
                index = -1;
            }
        }
    }

    public sealed class BlockList
    {
        private Block[] /*!*/
            elements;

        public BlockList()
        {
            elements = new Block[4];
            //^ base();
        }

        public BlockList(int n)
        {
            elements = new Block[n];
            //^ base();
        }

        public int Count { get; private set; }

        [Obsolete("Use Count property instead.")]
        public int Length => Count;

        public Block this[int index]
        {
            get { return elements[index]; }
            set { elements[index] = value; }
        }

        public void Add(Block element)
        {
            var n = elements.Length;
            var i = Count++;
            if (i == n)
            {
                var m = n * 2;
                if (m < 4) m = 4;
                var newElements = new Block[m];
                for (var j = 0; j < n; j++) newElements[j] = elements[j];
                elements = newElements;
            }

            elements[i] = element;
        }

        public BlockList /*!*/ Clone()
        {
            var elements = this.elements;
            var n = Count;
            var result = new BlockList(n);
            result.Count = n;
            var newElements = result.elements;
            for (var i = 0; i < n; i++)
                newElements[i] = elements[i];
            return result;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        public struct Enumerator
        {
            private int index;

            private readonly BlockList /*!*/
                list;

            public Enumerator(BlockList /*!*/ list)
            {
                index = -1;
                this.list = list;
            }

            public Block Current => list[index];

            public bool MoveNext()
            {
                return ++index < list.Count;
            }

            public void Reset()
            {
                index = -1;
            }
        }
    }
#if !MinimalReader && !CodeContracts
  public sealed class CatchList{
    private Catch[]/*!*/ elements;
    private int count = 0;
    public CatchList(){
      this.elements = new Catch[4];
      //^ base();
    }
    public CatchList(int n){
      this.elements = new Catch[n];
      //^ base();
    }
    public void Add(Catch element){
      int n = this.elements.Length;
      int i = this.count++;
      if (i == n){
        int m = n*2; if (m < 4) m = 4;
        Catch[] newElements = new Catch[m];
        for (int j = 0; j < n; j++) newElements[j] = elements[j];
        this.elements = newElements;
      }
      this.elements[i] = element;
    }
    public CatchList/*!*/ Clone() {
      Catch[] elements = this.elements;
      int n = this.count;
      CatchList result = new CatchList(n);
      result.count = n;
      Catch[] newElements = result.elements;
      for (int i = 0; i < n; i++)
        newElements[i] = elements[i];
      return result;
    }
    public int Count{
      get{return this.count;}
    }
    [Obsolete("Use Count property instead.")]
    public int Length{
      get{return this.count;}
    }
    public Catch this[int index]{
      get{
        return this.elements[index];
      }
      set{
        this.elements[index] = value;
      }
    }
    public Enumerator GetEnumerator(){
      return new Enumerator(this);
    }
    public struct Enumerator{
      private int index;
      private readonly CatchList/*!*/ list;
      public Enumerator(CatchList/*!*/ list) {
        this.index = -1;
        this.list = list;
      }
      public Catch Current{
        get{
          return this.list[this.index];
        }
      }
      public bool MoveNext(){
        return ++this.index < this.list.count;
      }
      public void Reset(){
        this.index = -1;
      }
    }
  }
  public sealed class CompilationList{
    private Compilation[]/*!*/ elements;
    private int count = 0;
    public CompilationList(){
      this.elements = new Compilation[4];
      //^ base();
    }
    public CompilationList(int n){
      this.elements = new Compilation[n];
      //^ base();
    }
    public CompilationList(params Compilation[] elements){
      if (elements == null) elements = new Compilation[0];
      this.elements = elements;
      this.count = elements.Length;
      //^ base();
    }
    public void Add(Compilation element){
      int n = this.elements.Length;
      int i = this.count++;
      if (i == n){
        int m = n*2; if (m < 4) m = 4;
        Compilation[] newElements = new Compilation[m];
        for (int j = 0; j < n; j++) newElements[j] = elements[j];
        this.elements = newElements;
      }
      this.elements[i] = element;
    }
    public CompilationList/*!*/ Clone() {
      Compilation[] elements = this.elements;
      int n = this.count;
      CompilationList result = new CompilationList(n);
      result.count = n;
      Compilation[] newElements = result.elements;
      for (int i = 0; i < n; i++)
        newElements[i] = elements[i];
      return result;
    }
    public int Count{
      get{return this.count;}
    }
    [Obsolete("Use Count property instead.")]
    public int Length{
      get{return this.count;}
    }
    public Compilation this[int index]{
      get{
        return this.elements[index];
      }
      set{
        this.elements[index] = value;
      }
    }
    public Enumerator GetEnumerator(){
      return new Enumerator(this);
    }
    public struct Enumerator{
      private int index;
      private readonly CompilationList/*!*/ list;
      public Enumerator(CompilationList/*!*/ list) {
        this.index = -1;
        this.list = list;
      }
      public Compilation Current{
        get{
          return this.list[this.index];
        }
      }
      public bool MoveNext(){
        return ++this.index < this.list.count;
      }
      public void Reset(){
        this.index = -1;
      }
    }
  }
  public sealed class CompilationUnitList{
    private CompilationUnit[]/*!*/ elements;
    private int count = 0;
    public CompilationUnitList(){
      this.elements = new CompilationUnit[4];
      //^ base();
    }
    public CompilationUnitList(int n){
      this.elements = new CompilationUnit[n];
      //^ base();
    }
    public CompilationUnitList(params CompilationUnit[] elements){
      if (elements == null) elements = new CompilationUnit[0];
      this.elements = elements;
      this.count = elements.Length;
      //^ base();
    }
    public void Add(CompilationUnit element){
      int n = this.elements.Length;
      int i = this.count++;
      if (i == n){
        int m = n*2; if (m < 4) m = 4;
        CompilationUnit[] newElements = new CompilationUnit[m];
        for (int j = 0; j < n; j++) newElements[j] = elements[j];
        this.elements = newElements;
      }
      this.elements[i] = element;
    }
    public CompilationUnitList/*!*/ Clone(){
      CompilationUnit[] elements = this.elements;
      int n = this.count;
      CompilationUnitList result = new CompilationUnitList(n);
      result.count = n;
      CompilationUnit[] newElements = result.elements;
      for (int i = 0; i < n; i++)
        newElements[i] = elements[i];
      return result;
    }
    public int Count{
      get{return this.count;}
    }
    [Obsolete("Use Count property instead.")]
    public int Length{
      get{return this.count;}
    }
    public CompilationUnit this[int index]{
      get{
        return this.elements[index];
      }
      set{
        this.elements[index] = value;
      }
    }
    public Enumerator GetEnumerator(){
      return new Enumerator(this);
    }
    public struct Enumerator{
      private int index;
      private readonly CompilationUnitList/*!*/ list;
      public Enumerator(CompilationUnitList/*!*/ list) {
        this.index = -1;
        this.list = list;
      }
      public CompilationUnit Current{
        get{
          return this.list[this.index];
        }
      }
      public bool MoveNext(){
        return ++this.index < this.list.count;
      }
      public void Reset(){
        this.index = -1;
      }
    }
  }
  public sealed class CompilationUnitSnippetList{
    private CompilationUnitSnippet[]/*!*/ elements;
    private int count = 0;
    public CompilationUnitSnippetList(){
      this.elements = new CompilationUnitSnippet[4];
      //^ base();
    }
    public CompilationUnitSnippetList(int n){
      this.elements = new CompilationUnitSnippet[n];
      //^ base();
    }
    public CompilationUnitSnippetList(params CompilationUnitSnippet[] elements){
      if (elements == null) elements = new CompilationUnitSnippet[0];
      this.elements = elements;
      this.count = elements.Length;
      //^ base();
    }
    public void Add(CompilationUnitSnippet element){
      int n = this.elements.Length;
      int i = this.count++;
      if (i == n){
        int m = n*2; if (m < 4) m = 4;
        CompilationUnitSnippet[] newElements = new CompilationUnitSnippet[m];
        for (int j = 0; j < n; j++) newElements[j] = elements[j];
        this.elements = newElements;
      }
      this.elements[i] = element;
    }
    public CompilationUnitSnippetList/*!*/ Clone() {
      CompilationUnitSnippet[] elements = this.elements;
      int n = this.count;
      CompilationUnitSnippetList result = new CompilationUnitSnippetList(n);
      result.count = n;
      CompilationUnitSnippet[] newElements = result.elements;
      for (int i = 0; i < n; i++)
        newElements[i] = elements[i];
      return result;
    }
    public int Count{
      get{return this.count;}
    }
    [Obsolete("Use Count property instead.")]
    public int Length{
      get{return this.count;}
    }
    public CompilationUnitSnippet this[int index]{
      get{
        return this.elements[index];
      }
      set{
        this.elements[index] = value;
      }
    }
    public Enumerator GetEnumerator(){
      return new Enumerator(this);
    }
    public struct Enumerator{
      private int index;
      private readonly CompilationUnitSnippetList/*!*/ list;
      public Enumerator(CompilationUnitSnippetList/*!*/ list) {
        this.index = -1;
        this.list = list;
      }
      public CompilationUnitSnippet Current{
        get{
          return this.list[this.index];
        }
      }
      public bool MoveNext(){
        return ++this.index < this.list.count;
      }
      public void Reset(){
        this.index = -1;
      }
    }
  }
#endif
#if !NoWriter
    public sealed class EventList
    {
        private Event[] /*!*/
            elements;

        public EventList()
        {
            elements = new Event[8];
            //^ base();
        }

        public EventList(int n)
        {
            elements = new Event[n];
            //^ base();
        }

        public int Count { get; private set; }

        [Obsolete("Use Count property instead.")]
        public int Length => Count;

        public Event this[int index]
        {
            get { return elements[index]; }
            set { elements[index] = value; }
        }

        public void Add(Event element)
        {
            var n = elements.Length;
            var i = Count++;
            if (i == n)
            {
                var m = n * 2;
                if (m < 8) m = 8;
                var newElements = new Event[m];
                for (var j = 0; j < n; j++) newElements[j] = elements[j];
                elements = newElements;
            }

            elements[i] = element;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        public struct Enumerator
        {
            private int index;

            private readonly EventList /*!*/
                list;

            public Enumerator(EventList /*!*/ list)
            {
                index = -1;
                this.list = list;
            }

            public Event Current => list[index];

            public bool MoveNext()
            {
                return ++index < list.Count;
            }

            public void Reset()
            {
                index = -1;
            }
        }
    }
#endif
#if !MinimalReader && !CodeContracts
  public sealed class ErrorNodeList{
    private ErrorNode[]/*!*/ elements;
    private int count = 0;
    public ErrorNodeList(){
      this.elements = new ErrorNode[8];
      //^ base();
    }
    public ErrorNodeList(int n){
      this.elements = new ErrorNode[n];
      //^ base();
    }
    public void Add(ErrorNode element){
      int n = this.elements.Length;
      int i = this.count++;
      if (i == n){
        int m = n*2; if (m < 8) m = 8;
        ErrorNode[] newElements = new ErrorNode[m];
        for (int j = 0; j < n; j++) newElements[j] = elements[j];
        this.elements = newElements;
      }
      this.elements[i] = element;
    }
    public int Count{
      get{return this.count;}
    }
    [Obsolete("Use Count property instead.")]
    public int Length{
      get{return this.count;}
    }
    public ErrorNode this[int index]{
      get{
        return this.elements[index];
      }
      set{
        this.elements[index] = value;
      }
    }
    public Enumerator GetEnumerator(){
      return new Enumerator(this);
    }
    public struct Enumerator{
      private int index;
      private readonly ErrorNodeList/*!*/ list;
      public Enumerator(ErrorNodeList/*!*/ list) {
        this.index = -1;
        this.list = list;
      }
      public ErrorNode Current{
        get{
          return this.list[this.index];
        }
      }
      public bool MoveNext(){
        return ++this.index < this.list.count;
      }
      public void Reset(){
        this.index = -1;
      }
    }
    private int FindNextEntry(ErrorNode[]/*!*/ errors, int index, int count, ErrorNodeComparer comparer) 
    {
      int res = -1;
      for(int i = index; i < index + count; i++) {
            if (errors[i] != null  && 
                !errors[i].GetMessage(System.Threading.Thread.CurrentThread.CurrentUICulture).Contains("(trace position)")) {
              //we have found an error to consider
              if(res == -1 || comparer.Compare(errors[res],errors[i]) > 0)
                res = i;
            }
          }
      return res;  
    }
    public void Sort(int index, int count)
    {
          // we cannot simply call Array.Sort because we have to make sure that trace positions
          // stay with their error messages.
          ErrorNode[]/*!*/ to = new ErrorNode[elements.Length];
          Array.Copy(elements, to, elements.Length);

          int free = index;
          int next = index;
          ErrorNode cur;
          ErrorNodeComparer comparer = new ErrorNodeComparer();
          while ((next = FindNextEntry(to, index, count, comparer)) != -1)
          {
              do {
                  cur = to[next];
                  to[next++] = null;
                  elements[free++] = cur;
              } while(next < index + count &&      // repeat until we do not have a related info
                      to[next] != null &&
                      to[next].GetMessage(System.Threading.Thread.CurrentThread.CurrentUICulture).Contains("(trace position)"));
          }
    }
  }

  public class ErrorNodeComparer : System.Collections.Generic.IComparer<ErrorNode/*!*/>
  {
      public int Compare(ErrorNode/*!*/ e1, ErrorNode/*!*/ e2)
      {
          if (e1.SourceContext.Document == null || e2.SourceContext.Document == null) return 0;

          int loc1 = e1.SourceContext.StartLine * 1000 + e1.SourceContext.StartColumn;
          int loc2 = e2.SourceContext.StartLine * 1000 + e2.SourceContext.StartColumn;
          if (loc1 == loc2)
          {
              string message1 = e1.GetMessage(System.Threading.Thread.CurrentThread.CurrentUICulture);
              string message2 = e2.GetMessage(System.Threading.Thread.CurrentThread.CurrentUICulture);
              return String.Compare(message1, message2, false, System.Globalization.CultureInfo.InvariantCulture);
          }
          if (loc1 > loc2) return 1;
          return -1;
      }
  }
#endif
    public sealed class ExpressionList
    {
        private Expression[] /*!*/
            elements;

        public ExpressionList()
        {
            elements = new Expression[8];
            //^ base();
        }

        public ExpressionList(int n)
        {
            elements = new Expression[n];
            //^ base();
        }

        public ExpressionList(params Expression[] elements)
        {
            if (elements == null) elements = new Expression[0];
            this.elements = elements;
            Count = elements.Length;
            //^ base();
        }

        public int Count { get; set; }

        [Obsolete("Use Count property instead.")]
        public int Length
        {
            get { return Count; }
            set { Count = value; }
        }

        public Expression this[int index]
        {
            get { return elements[index]; }
            set { elements[index] = value; }
        }

        public void Add(Expression element)
        {
            var n = elements.Length;
            var i = Count++;
            if (i == n)
            {
                var m = n * 2;
                if (m < 8) m = 8;
                var newElements = new Expression[m];
                for (var j = 0; j < n; j++) newElements[j] = elements[j];
                elements = newElements;
            }

            elements[i] = element;
        }

        public ExpressionList /*!*/ Clone()
        {
            var elements = this.elements;
            var n = Count;
            var result = new ExpressionList(n);
            result.Count = n;
            var newElements = result.elements;
            for (var i = 0; i < n; i++)
                newElements[i] = elements[i];
            return result;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        public struct Enumerator
        {
            private int index;

            private readonly ExpressionList /*!*/
                list;

            public Enumerator(ExpressionList /*!*/ list)
            {
                index = -1;
                this.list = list;
            }

            public Expression Current => list[index];

            public bool MoveNext()
            {
                return ++index < list.Count;
            }

            public void Reset()
            {
                index = -1;
            }
        }
    }

    public sealed class ExceptionHandlerList
    {
        private ExceptionHandler[] /*!*/
            elements = new ExceptionHandler[4];

        public ExceptionHandlerList()
        {
            //^ base();
        }

        public ExceptionHandlerList(int n)
        {
            elements = new ExceptionHandler[n];
            //^ base();
        }

        public int Count { get; private set; }

        [Obsolete("Use Count property instead.")]
        public int Length => Count;

        public ExceptionHandler this[int index]
        {
            get { return elements[index]; }
            set { elements[index] = value; }
        }

        public void Add(ExceptionHandler element)
        {
            var n = elements.Length;
            var i = Count++;
            if (i == n)
            {
                var m = n * 2;
                if (m < 4) m = 4;
                var newElements = new ExceptionHandler[m];
                for (var j = 0; j < n; j++) newElements[j] = elements[j];
                elements = newElements;
            }

            elements[i] = element;
        }

        public ExceptionHandlerList /*!*/ Clone()
        {
            var elements = this.elements;
            var n = Count;
            var result = new ExceptionHandlerList(n);
            result.Count = n;
            var newElements = result.elements;
            for (var i = 0; i < n; i++)
                newElements[i] = elements[i];
            return result;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        public struct Enumerator
        {
            private int index;

            private readonly ExceptionHandlerList /*!*/
                list;

            public Enumerator(ExceptionHandlerList /*!*/ list)
            {
                index = -1;
                this.list = list;
            }

            public ExceptionHandler Current => list[index];

            public bool MoveNext()
            {
                return ++index < list.Count;
            }

            public void Reset()
            {
                index = -1;
            }
        }
    }
#if !MinimalReader
    public sealed class FaultHandlerList
    {
        private FaultHandler[] /*!*/
            elements;

        public FaultHandlerList()
        {
            elements = new FaultHandler[4];
            //^ base();
        }

        public FaultHandlerList(int n)
        {
            elements = new FaultHandler[n];
            //^ base();
        }

        public int Count { get; private set; }

        [Obsolete("Use Count property instead.")]
        public int Length => Count;

        public FaultHandler this[int index]
        {
            get { return elements[index]; }
            set { elements[index] = value; }
        }

        public void Add(FaultHandler element)
        {
            var n = elements.Length;
            var i = Count++;
            if (i == n)
            {
                var m = n * 2;
                if (m < 4) m = 4;
                var newElements = new FaultHandler[m];
                for (var j = 0; j < n; j++) newElements[j] = elements[j];
                elements = newElements;
            }

            elements[i] = element;
        }

        public FaultHandlerList /*!*/ Clone()
        {
            var elements = this.elements;
            var n = Count;
            var result = new FaultHandlerList(n);
            result.Count = n;
            var newElements = result.elements;
            for (var i = 0; i < n; i++)
                newElements[i] = elements[i];
            return result;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        public struct Enumerator
        {
            private int index;

            private readonly FaultHandlerList /*!*/
                list;

            public Enumerator(FaultHandlerList /*!*/ list)
            {
                index = -1;
                this.list = list;
            }

            public FaultHandler Current => list[index];

            public bool MoveNext()
            {
                return ++index < list.Count;
            }

            public void Reset()
            {
                index = -1;
            }
        }
    }
#endif
#if !NoWriter || !MinimalReader
    public sealed class FieldList
    {
        private Field[] /*!*/
            elements;

        public FieldList()
        {
            elements = new Field[8];
            //^ base();
        }

        public FieldList(int capacity)
        {
            elements = new Field[capacity];
            //^ base();
        }

        public FieldList(params Field[] elements)
        {
            if (elements == null) elements = new Field[0];
            this.elements = elements;
            Count = elements.Length;
            //^ base();
        }

        public int Count { get; private set; }

        [Obsolete("Use Count property instead.")]
        public int Length => Count;

        public Field this[int index]
        {
            get { return elements[index]; }
            set { elements[index] = value; }
        }

        public void Add(Field element)
        {
            var n = elements.Length;
            var i = Count++;
            if (i == n)
            {
                var m = n * 2;
                if (m < 8) m = 8;
                var newElements = new Field[m];
                for (var j = 0; j < n; j++) newElements[j] = elements[j];
                elements = newElements;
            }

            elements[i] = element;
        }

        public FieldList /*!*/ Clone()
        {
            var elements = this.elements;
            var n = Count;
            var result = new FieldList(n);
            result.Count = n;
            var newElements = result.elements;
            for (var i = 0; i < n; i++)
                newElements[i] = elements[i];
            return result;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        public struct Enumerator
        {
            private int index;

            private readonly FieldList /*!*/
                list;

            public Enumerator(FieldList /*!*/list)
            {
                index = -1;
                this.list = list;
            }

            public Field Current => list[index];

            public bool MoveNext()
            {
                return ++index < list.Count;
            }

            public void Reset()
            {
                index = -1;
            }
        }
    }
#endif
#if !MinimalReader
    public sealed class FilterList
    {
        private Filter[] /*!*/
            elements;

        public FilterList()
        {
            elements = new Filter[4];
            //^ base();
        }

        public FilterList(int capacity)
        {
            elements = new Filter[capacity];
            //^ base();
        }

        public int Count { get; private set; }

        [Obsolete("Use Count property instead.")]
        public int Length => Count;

        public Filter this[int index]
        {
            get { return elements[index]; }
            set { elements[index] = value; }
        }

        public void Add(Filter element)
        {
            var n = elements.Length;
            var i = Count++;
            if (i == n)
            {
                var m = n * 2;
                if (m < 4) m = 4;
                var newElements = new Filter[m];
                for (var j = 0; j < n; j++) newElements[j] = elements[j];
                elements = newElements;
            }

            elements[i] = element;
        }

        public FilterList /*!*/ Clone()
        {
            var elements = this.elements;
            var n = Count;
            var result = new FilterList(n);
            result.Count = n;
            var newElements = result.elements;
            for (var i = 0; i < n; i++)
                newElements[i] = elements[i];
            return result;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        public struct Enumerator
        {
            private int index;

            private readonly FilterList /*!*/
                list;

            public Enumerator(FilterList /*!*/ list)
            {
                index = -1;
                this.list = list;
            }

            public Filter Current => list[index];

            public bool MoveNext()
            {
                return ++index < list.Count;
            }

            public void Reset()
            {
                index = -1;
            }
        }
    }

    public sealed class IdentifierList
    {
        private Identifier[] /*!*/
            elements;

        public IdentifierList()
        {
            elements = new Identifier[8];
            //^ base();
        }

        public IdentifierList(int capacity)
        {
            elements = new Identifier[capacity];
            //^ base();
        }

        public int Count { get; set; }

        [Obsolete("Use Count property instead.")]
        public int Length
        {
            get { return Count; }
            set { Count = value; }
        }

        public Identifier this[int index]
        {
            get { return elements[index]; }
            set { elements[index] = value; }
        }

        public void Add(Identifier element)
        {
            var n = elements.Length;
            var i = Count++;
            if (i == n)
            {
                var m = n * 2;
                if (m < 8) m = 8;
                var newElements = new Identifier[m];
                for (var j = 0; j < n; j++) newElements[j] = elements[j];
                elements = newElements;
            }

            elements[i] = element;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        public struct Enumerator
        {
            private int index;

            private readonly IdentifierList /*!*/
                list;

            public Enumerator(IdentifierList /*!*/ list)
            {
                index = -1;
                this.list = list;
            }

            public Identifier Current => list[index];

            public bool MoveNext()
            {
                return ++index < list.Count;
            }

            public void Reset()
            {
                index = -1;
            }
        }
    }
#endif
    public sealed class InstructionList
    {
        private Instruction[] /*!*/
            elements;

        public InstructionList()
        {
            elements = new Instruction[32];
            //^ base();
        }

        public InstructionList(int capacity)
        {
            elements = new Instruction[capacity];
            //^ base();
        }

        public int Count { get; set; }

        [Obsolete("Use Count property instead.")]
        public int Length
        {
            get { return Count; }
            set { Count = value; }
        }

        public Instruction this[int index]
        {
            get { return elements[index]; }
            set { elements[index] = value; }
        }

        public void Add(Instruction element)
        {
            var n = elements.Length;
            var i = Count++;
            if (i == n)
            {
                var m = n * 2;
                if (m < 32) m = 32;
                var newElements = new Instruction[m];
                for (var j = 0; j < n; j++) newElements[j] = elements[j];
                elements = newElements;
            }

            elements[i] = element;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        public struct Enumerator
        {
            private int index;

            private readonly InstructionList /*!*/
                list;

            public Enumerator(InstructionList /*!*/ list)
            {
                index = -1;
                this.list = list;
            }

            public Instruction Current => list[index];

            public bool MoveNext()
            {
                return ++index < list.Count;
            }

            public void Reset()
            {
                index = -1;
            }
        }
    }

    public sealed class InterfaceList
    {
        private AttributeList[] attributes;

        private Interface[] /*!*/
            elements;

        public InterfaceList()
        {
            elements = new Interface[8];
            //^ base();
        }

        public InterfaceList(int capacity)
        {
            elements = new Interface[capacity];
            //^ base();
        }

        public InterfaceList(params Interface[] elements)
        {
            if (elements == null) elements = new Interface[0];
            this.elements = elements;
            Count = elements.Length;
            //^ base();
        }

        public int Count { get; set; }

        [Obsolete("Use Count property instead.")]
        public int Length
        {
            get { return Count; }
            set { Count = value; }
        }

        public Interface this[int index]
        {
            get { return elements[index]; }
            set { elements[index] = value; }
        }

        public void Add(Interface element)
        {
            var n = elements.Length;
            var i = Count++;
            if (i == n)
            {
                var m = n * 2;
                if (m < 8) m = 8;
                var newElements = new Interface[m];
                for (var j = 0; j < n; j++) newElements[j] = elements[j];
                elements = newElements;
                if (attributes != null)
                {
                    var newAttributes = new AttributeList[m];
                    for (var j = 0; j < n; j++) newAttributes[j] = attributes[j];
                    attributes = newAttributes;
                }
            }

            elements[i] = element;
        }

        public void AddAttributes(int index, AttributeList attributes)
        {
            if (this.attributes == null) this.attributes = new AttributeList[elements.Length];
            this.attributes[index] = attributes;
        }

        public AttributeList /*?*/ AttributesFor(int index)
        {
            if (attributes == null) return null;
            return attributes[index];
        }

        public InterfaceList /*!*/ Clone()
        {
            var elements = this.elements;
            var n = Count;
            var result = new InterfaceList(n);
            result.Count = n;
            var newElements = result.elements;
            for (var i = 0; i < n; i++)
                newElements[i] = elements[i];
            return result;
        }

        public int SearchFor(Interface element)
        {
            var elements = this.elements;
            for (int i = 0, n = Count; i < n; i++)
                if (elements[i] == (object)element)
                    return i;
            return -1;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        public struct Enumerator
        {
            private int index;

            private readonly InterfaceList /*!*/
                list;

            public Enumerator(InterfaceList /*!*/ list)
            {
                index = -1;
                this.list = list;
            }

            public Interface Current => list[index];

            public bool MoveNext()
            {
                return ++index < list.Count;
            }

            public void Reset()
            {
                index = -1;
            }
        }
    }
#if ExtendedRuntime || CodeContracts
    public sealed class InvariantList
    {
        private Invariant[] /*!*/
            elements;

        public InvariantList()
        {
            elements = new Invariant[8];
            //^ base();
        }

        public InvariantList(int n)
        {
            elements = new Invariant[n];
            //^ base();
        }

        public InvariantList(params Invariant[] elements)
        {
            if (elements == null) elements = new Invariant[0];
            this.elements = elements;
            Count = elements.Length;
            //^ base();
        }

        public int Count { get; set; }

        [Obsolete("Use Count property instead.")]
        public int Length
        {
            get { return Count; }
            set { Count = value; }
        }

        public Invariant this[int index]
        {
            get { return elements[index]; }
            set { elements[index] = value; }
        }

        public void Add(Invariant element)
        {
            var n = elements.Length;
            var i = Count++;
            if (i == n)
            {
                var m = n * 2;
                if (m < 8) m = 8;
                var newElements = new Invariant[m];
                for (var j = 0; j < n; j++) newElements[j] = elements[j];
                elements = newElements;
            }

            elements[i] = element;
        }

        public InvariantList /*!*/ Clone()
        {
            var elements = this.elements;
            var n = Count;
            var result = new InvariantList(n);
            result.Count = n;
            var newElements = result.elements;
            for (var i = 0; i < n; i++)
                newElements[i] = elements[i];
            return result;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        public struct Enumerator
        {
            private int index;

            private readonly InvariantList /*!*/
                list;

            public Enumerator(InvariantList /*!*/ list)
            {
                index = -1;
                this.list = list;
            }

            public Invariant Current => list[index];

            public bool MoveNext()
            {
                return ++index < list.Count;
            }

            public void Reset()
            {
                index = -1;
            }
        }
    }
#endif
    public sealed class Int32List
    {
        private int[] /*!*/
            elements;

        public Int32List()
        {
            elements = new int[8];
            //^ base();
        }

        public Int32List(int capacity)
        {
            elements = new int[capacity];
            //^ base();
        }

        public Int32List(params int[] elements)
        {
            if (elements == null) elements = new int[0];
            this.elements = elements;
            Count = elements.Length;
            //^ base();
        }

        public int Count { get; private set; }

        [Obsolete("Use Count property instead.")]
        public int Length => Count;

        public int this[int index]
        {
            get { return elements[index]; }
            set { elements[index] = value; }
        }

        public void Add(int element)
        {
            var n = elements.Length;
            var i = Count++;
            if (i == n)
            {
                var m = n * 2;
                if (m < 8) m = 8;
                var newElements = new int[m];
                for (var j = 0; j < n; j++) newElements[j] = elements[j];
                elements = newElements;
            }

            elements[i] = element;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        public struct Enumerator
        {
            private int index;

            private readonly Int32List /*!*/
                list;

            public Enumerator(Int32List /*!*/ list)
            {
                index = -1;
                this.list = list;
            }

            public int Current => list[index];

            public bool MoveNext()
            {
                return ++index < list.Count;
            }

            public void Reset()
            {
                index = -1;
            }
        }
    }
#if !MinimalReader && !CodeContracts
  public sealed class ISourceTextList{
    private ISourceText[]/*!*/ elements = new ISourceText[4];
    private int count = 0;
    public ISourceTextList(){
      this.elements = new ISourceText[4];
      //^ base();
    }
    public ISourceTextList(int capacity){
      this.elements = new ISourceText[capacity];
      //^ base();
    }
    public ISourceTextList(params ISourceText[] elements){
      if (elements == null) elements = new ISourceText[0];
      this.elements = elements;
      this.count = elements.Length;
      //^ base();
    }
    public void Add(ISourceText element){
      int n = this.elements.Length;
      int i = this.count++;
      if (i == n){
        int m = n*2; if (m < 4) m = 4;
        ISourceText[] newElements = new ISourceText[m];
        for (int j = 0; j < n; j++) newElements[j] = elements[j];
        this.elements = newElements;
      }
      this.elements[i] = element;
    }
    public int Count{
      get{return this.count;}
    }
    [Obsolete("Use Count property instead.")]
    public int Length{
      get{return this.count;}
    }
    public ISourceText this[int index]{
      get{
        return this.elements[index];
      }
      set{
        this.elements[index] = value;
      }
    }
    public Enumerator GetEnumerator(){
      return new Enumerator(this);
    }
    public struct Enumerator{
      private int index;
      private readonly ISourceTextList/*!*/ list;
      public Enumerator(ISourceTextList/*!*/ list) {
        this.index = -1;
        this.list = list;
      }
      public ISourceText Current{
        get{
          return this.list[this.index];
        }
      }
      public bool MoveNext(){
        return ++this.index < this.list.count;
      }
      public void Reset(){
        this.index = -1;
      }
    }
  }
  public sealed class LocalDeclarationList{
    private LocalDeclaration[]/*!*/ elements;
    private int count = 0;
    public LocalDeclarationList(){
      this.elements = new LocalDeclaration[8];
      //^ base();
    }
    public LocalDeclarationList(int capacity){
      this.elements = new LocalDeclaration[capacity];
      //^ base();
    }
    public void Add(LocalDeclaration element){
      int n = this.elements.Length;
      int i = this.count++;
      if (i == n){
        int m = n*2; if (m < 8) m = 8;
        LocalDeclaration[] newElements = new LocalDeclaration[m];
        for (int j = 0; j < n; j++) newElements[j] = elements[j];
        this.elements = newElements;
      }
      this.elements[i] = element;
    }
    public LocalDeclarationList/*!*/ Clone() {
      LocalDeclaration[] elements = this.elements;
      int n = this.count;
      LocalDeclarationList result = new LocalDeclarationList(n);
      result.count = n;
      LocalDeclaration[] newElements = result.elements;
      for (int i = 0; i < n; i++)
        newElements[i] = elements[i];
      return result;
    }
    public int Count{
      get{return this.count;}
    }
    [Obsolete("Use Count property instead.")]
    public int Length{
      get{return this.count;}
    }
    public LocalDeclaration this[int index]{
      get{
        return this.elements[index];
      }
      set{
        this.elements[index] = value;
      }
    }
    public Enumerator GetEnumerator(){
      return new Enumerator(this);
    }
    public struct Enumerator{
      private int index;
      private readonly LocalDeclarationList/*!*/ list;
      public Enumerator(LocalDeclarationList/*!*/ list) {
        this.index = -1;
        this.list = list;
      }
      public LocalDeclaration Current{
        get{
          return this.list[this.index];
        }
      }
      public bool MoveNext(){
        return ++this.index < this.list.count;
      }
      public void Reset(){
        this.index = -1;
      }
    }
  }
#endif
#if ExtendedRuntime || CodeContracts
    public sealed class RequiresList
    {
        private Requires[] /*!*/
            elements;

        public RequiresList()
        {
            elements = new Requires[2];
            //^ base();
        }

        public RequiresList(int capacity)
        {
            elements = new Requires[capacity];
            //^ base();
        }

        public int Count { get; private set; }

        [Obsolete("Use Count property instead.")]
        public int Length => Count;

        public Requires this[int index]
        {
            get
            {
#if CLOUSOT
                Contract.Requires(index >= 0);
                Contract.Requires(index < Count);
#endif
                return elements[index];
            }
            set { elements[index] = value; }
        }

        public void Add(Requires element)
        {
            var n = elements.Length;
            var i = Count++;
            if (i == n)
            {
                var m = n * 2;
                if (m < 8) m = 8;
                var newElements = new Requires[m];
                for (var j = 0; j < n; j++) newElements[j] = elements[j];
                elements = newElements;
            }

            elements[i] = element;
        }

        public RequiresList /*!*/ Clone()
        {
            var elements = this.elements;
            var n = Count;
            var result = new RequiresList(n);
            result.Count = n;
            var newElements = result.elements;
            for (var i = 0; i < n; i++)
                newElements[i] = elements[i];
            return result;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        public struct Enumerator
        {
            private int index;

            private readonly RequiresList /*!*/
                list;

            public Enumerator(RequiresList /*!*/ list)
            {
                index = -1;
                this.list = list;
            }

            public Requires Current => list[index];

            public bool MoveNext()
            {
                return ++index < list.Count;
            }

            public void Reset()
            {
                index = -1;
            }
        }
    }

    public sealed class EnsuresList
    {
        private Ensures[] /*!*/
            elements;

        public EnsuresList()
        {
            elements = new Ensures[2];
            //^ base();
        }

        public EnsuresList(int capacity)
        {
            elements = new Ensures[capacity];
            //^ base();
        }

        public int Count { get; private set; }

        [Obsolete("Use Count property instead.")]
        public int Length => Count;

        public Ensures this[int index]
        {
            get { return elements[index]; }
            set { elements[index] = value; }
        }

        public void Add(Ensures element)
        {
            var n = elements.Length;
            var i = Count++;
            if (i == n)
            {
                var m = n * 2;
                if (m < 8) m = 8;
                var newElements = new Ensures[m];
                for (var j = 0; j < n; j++) newElements[j] = elements[j];
                elements = newElements;
            }

            elements[i] = element;
        }

        public EnsuresList /*!*/ Clone()
        {
            var elements = this.elements;
            var n = Count;
            var result = new EnsuresList(n);
            result.Count = n;
            var newElements = result.elements;
            for (var i = 0; i < n; i++)
                newElements[i] = elements[i];
            return result;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        public struct Enumerator
        {
            private int index;

            private readonly EnsuresList /*!*/
                list;

            public Enumerator(EnsuresList /*!*/ list)
            {
                index = -1;
                this.list = list;
            }

            public Ensures Current => list[index];

            public bool MoveNext()
            {
                return ++index < list.Count;
            }

            public void Reset()
            {
                index = -1;
            }
        }
    }

    public sealed class MethodContractElementList
    {
        private MethodContractElement[] /*!*/
            elements;

        public MethodContractElementList()
        {
            elements = new MethodContractElement[2];
            //^ base();
        }

        public MethodContractElementList(int capacity)
        {
            elements = new MethodContractElement[capacity];
            //^ base();
        }

        public int Count { get; private set; }

        public MethodContractElement this[int index]
        {
            get { return elements[index]; }
            set { elements[index] = value; }
        }

        public void Add(MethodContractElement element)
        {
            var n = elements.Length;
            var i = Count++;
            if (i == n)
            {
                var m = n * 2;
                if (m < 8) m = 8;
                var newElements = new MethodContractElement[m];
                for (var j = 0; j < n; j++) newElements[j] = elements[j];
                elements = newElements;
            }

            elements[i] = element;
        }

        public MethodContractElementList /*!*/ Clone()
        {
            var elements = this.elements;
            var n = Count;
            var result = new MethodContractElementList(n);
            result.Count = n;
            var newElements = result.elements;
            for (var i = 0; i < n; i++)
                newElements[i] = elements[i];
            return result;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        public struct Enumerator
        {
            private int index;

            private readonly MethodContractElementList /*!*/
                list;

            public Enumerator(MethodContractElementList /*!*/ list)
            {
                index = -1;
                this.list = list;
            }

            public MethodContractElement Current => list[index];

            public bool MoveNext()
            {
                return ++index < list.Count;
            }

            public void Reset()
            {
                index = -1;
            }
        }
    }
#endif
    public sealed class LocalList
    {
        private Local[] /*!*/
            elements;

        public LocalList()
        {
            elements = new Local[8];
            //^ base();
        }

        public LocalList(int capacity)
        {
            elements = new Local[capacity];
            //^ base();
        }

        public int Count { get; private set; }

        [Obsolete("Use Count property instead.")]
        public int Length => Count;

        public Local this[int index]
        {
            get { return elements[index]; }
            set { elements[index] = value; }
        }

        public void Add(Local element)
        {
            var n = elements.Length;
            var i = Count++;
            if (i == n)
            {
                var m = n * 2;
                if (m < 8) m = 8;
                var newElements = new Local[m];
                for (var j = 0; j < n; j++) newElements[j] = elements[j];
                elements = newElements;
            }

            elements[i] = element;
        }

        public struct Enumerator
        {
            private int index;

            private readonly LocalList /*!*/
                list;

            public Enumerator(LocalList /*!*/ list)
            {
                index = -1;
                this.list = list;
            }

            public Local Current => list[index];

            public bool MoveNext()
            {
                return ++index < list.Count;
            }

            public void Reset()
            {
                index = -1;
            }
        }
    }

    public sealed class MemberList
    {
        private Member[] /*!*/
            elements;

        public MemberList()
        {
            elements = new Member[16];
            //^ base();
        }

        public MemberList(int capacity)
        {
            elements = new Member[capacity];
            //^ base();
        }

        public MemberList(params Member[] elements)
        {
            if (elements == null) elements = new Member[0];
            this.elements = elements;
            Count = elements.Length;
            //^ base();
        }

        public int Count { get; private set; }

        [Obsolete("Use Count property instead.")]
        public int Length => Count;

        public Member this[int index]
        {
            get { return elements[index]; }
            set { elements[index] = value; }
        }

        public void Add(Member element)
        {
            var n = elements.Length;
            var i = Count++;
            if (i == n)
            {
                var m = n * 2;
                if (m < 16) m = 16;
                var newElements = new Member[m];
                for (var j = 0; j < n; j++) newElements[j] = elements[j];
                elements = newElements;
            }

            elements[i] = element;
        }

        public void RemoveAt(int index)
        {
            if (index >= Count || index < 0) return;
            var n = Count;
            for (var i = index + 1; i < n; ++i)
                elements[i - 1] = elements[i];
            Count--;
        }

        public MemberList /*!*/ Clone()
        {
            var elements = this.elements;
            var n = Count;
            var result = new MemberList(n);
            result.Count = n;
            var newElements = result.elements;
            for (var i = 0; i < n; i++)
                newElements[i] = elements[i];
            return result;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        public Member[] /*!*/ ToArray()
        {
            var m = new Member[Count];
            Array.Copy(elements, m, Count);
            return m;
        }

        public struct Enumerator
        {
            private int index;

            private readonly MemberList /*!*/
                list;

            public Enumerator(MemberList /*!*/ list)
            {
                index = -1;
                this.list = list;
            }

            public Member Current => list[index];

            public bool MoveNext()
            {
                return ++index < list.Count;
            }

            public void Reset()
            {
                index = -1;
            }
        }
#if !MinimalReader
        public bool Contains(Member element)
        {
            var n = Count;
            for (var i = 0; i < n; i++)
                if (elements[i] == element)
                    return true;
            return false;
        }

        public void AddList(MemberList memberList)
        {
            if (memberList == null || memberList.Count == 0) return;
            var n = elements.Length;
            var newN = Count + memberList.Count;
            if (newN > n)
            {
                var m = newN;
                if (m < 16) m = 16;
                var newElements = new Member[m];
                for (var j = 0; j < n; j++) newElements[j] = elements[j];
                elements = newElements;
            }

            for (int i = Count, j = 0; i < newN; ++i, ++j) elements[i] = memberList.elements[j];
            Count = newN;
        }

        /// <summary>
        ///     Removes member (by nulling slot) if present
        /// </summary>
        public void Remove(Member member)
        {
            var n = Count;
            for (var i = 0; i < n; i++)
                if (elements[i] == member)
                {
                    elements[i] = null;
                    return;
                }
        }
#endif
    }
#if !MinimalReader
    public sealed class MemberBindingList
    {
        private MemberBinding[] /*!*/
            elements;

        public MemberBindingList()
        {
            elements = new MemberBinding[8];
            //^ base();
        }

        public MemberBindingList(int capacity)
        {
            elements = new MemberBinding[capacity];
            //^ base();
        }

        public int Count { get; private set; }

        [Obsolete("Use Count property instead.")]
        public int Length => Count;

        public MemberBinding this[int index]
        {
            get { return elements[index]; }
            set { elements[index] = value; }
        }

        public void Add(MemberBinding element)
        {
            var n = elements.Length;
            var i = Count++;
            if (i == n)
            {
                var m = n * 2;
                if (m < 8) m = 8;
                var newElements = new MemberBinding[m];
                for (var j = 0; j < n; j++) newElements[j] = elements[j];
                elements = newElements;
            }

            elements[i] = element;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        public struct Enumerator
        {
            private int index;

            private readonly MemberBindingList /*!*/
                list;

            public Enumerator(MemberBindingList /*!*/ list)
            {
                index = -1;
                this.list = list;
            }

            public MemberBinding Current => list[index];

            public bool MoveNext()
            {
                return ++index < list.Count;
            }

            public void Reset()
            {
                index = -1;
            }
        }
    }
#endif
    public sealed class MethodList
    {
        private Method[] /*!*/
            elements;

        public MethodList()
        {
            elements = new Method[8];
            //^ base();
        }

        public MethodList(int capacity)
        {
            elements = new Method[capacity];
            //^ base();
        }

        public MethodList(params Method[] elements)
        {
            if (elements == null) elements = new Method[0];
            this.elements = elements;
            Count = elements.Length;
            //^ base();
        }

        public int Count { get; private set; }

        [Obsolete("Use Count property instead.")]
        public int Length => Count;

        public Method this[int index]
        {
            get { return elements[index]; }
            set { elements[index] = value; }
        }

        public void Add(Method element)
        {
            var n = elements.Length;
            var i = Count++;
            if (i == n)
            {
                var m = n * 2;
                if (m < 8) m = 8;
                var newElements = new Method[m];
                for (var j = 0; j < n; j++) newElements[j] = elements[j];
                elements = newElements;
            }

            elements[i] = element;
        }

        public MethodList /*!*/ Clone()
        {
            var elements = this.elements;
            var n = Count;
            var result = new MethodList(n);
            result.Count = n;
            var newElements = result.elements;
            for (var i = 0; i < n; i++)
                newElements[i] = elements[i];
            return result;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        public struct Enumerator
        {
            private int index;

            private readonly MethodList /*!*/
                list;

            public Enumerator(MethodList /*!*/ list)
            {
                index = -1;
                this.list = list;
            }

            public Method Current => list[index];

            public bool MoveNext()
            {
                return ++index < list.Count;
            }

            public void Reset()
            {
                index = -1;
            }
        }
    }
#if !NoWriter
    public sealed class ModuleList
    {
        private Module[] /*!*/
            elements;

        public ModuleList()
        {
            elements = new Module[4];
            //^ base();
        }

        public ModuleList(int capacity)
        {
            elements = new Module[capacity];
            //^ base();
        }

        public int Count { get; private set; }

        [Obsolete("Use Count property instead.")]
        public int Length => Count;

        public Module this[int index]
        {
            get { return elements[index]; }
            set { elements[index] = value; }
        }

        public void Add(Module element)
        {
            var n = elements.Length;
            var i = Count++;
            if (i == n)
            {
                var m = n * 2;
                if (m < 4) m = 4;
                var newElements = new Module[m];
                for (var j = 0; j < n; j++) newElements[j] = elements[j];
                elements = newElements;
            }

            elements[i] = element;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        public struct Enumerator
        {
            private int index;

            private readonly ModuleList /*!*/
                list;

            public Enumerator(ModuleList /*!*/ list)
            {
                index = -1;
                this.list = list;
            }

            public Module Current => list[index];

            public bool MoveNext()
            {
                return ++index < list.Count;
            }

            public void Reset()
            {
                index = -1;
            }
        }
    }
#endif
    public sealed class ModuleReferenceList
    {
        private ModuleReference[] /*!*/
            elements;

        public ModuleReferenceList()
        {
            elements = new ModuleReference[4];
            //^ base();
        }

        public ModuleReferenceList(int capacity)
        {
            elements = new ModuleReference[capacity];
            //^ base();
        }

        public ModuleReferenceList(params ModuleReference[] elements)
        {
            if (elements == null) elements = new ModuleReference[0];
            this.elements = elements;
            Count = elements.Length;
            //^ base();
        }

        public int Count { get; private set; }

        [Obsolete("Use Count property instead.")]
        public int Length => Count;

        public ModuleReference this[int index]
        {
            get { return elements[index]; }
            set { elements[index] = value; }
        }

        public void Add(ModuleReference element)
        {
            var n = elements.Length;
            var i = Count++;
            if (i == n)
            {
                var m = n * 2;
                if (m < 4) m = 4;
                var newElements = new ModuleReference[m];
                for (var j = 0; j < n; j++) newElements[j] = elements[j];
                elements = newElements;
            }

            elements[i] = element;
        }

        public ModuleReferenceList /*!*/ Clone()
        {
            var elements = this.elements;
            var n = Count;
            var result = new ModuleReferenceList(n);
            result.Count = n;
            var newElements = result.elements;
            for (var i = 0; i < n; i++)
                newElements[i] = elements[i];
            return result;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        public struct Enumerator
        {
            private int index;

            private readonly ModuleReferenceList /*!*/
                list;

            public Enumerator(ModuleReferenceList /*!*/ list)
            {
                index = -1;
                this.list = list;
            }

            public ModuleReference Current => list[index];

            public bool MoveNext()
            {
                return ++index < list.Count;
            }

            public void Reset()
            {
                index = -1;
            }
        }
    }

    public sealed class NamespaceList
    {
        private Namespace[] /*!*/
            elements;

        public NamespaceList()
        {
            elements = new Namespace[4];
            //^ base();
        }

        public NamespaceList(int capacity)
        {
            elements = new Namespace[capacity];
            //^ base();
        }

        public int Count { get; private set; }

        [Obsolete("Use Count property instead.")]
        public int Length => Count;

        public Namespace this[int index]
        {
            get { return elements[index]; }
            set { elements[index] = value; }
        }

        public void Add(Namespace element)
        {
            var n = elements.Length;
            var i = Count++;
            if (i == n)
            {
                var m = n * 2;
                if (m < 4) m = 4;
                var newElements = new Namespace[m];
                for (var j = 0; j < n; j++) newElements[j] = elements[j];
                elements = newElements;
            }

            elements[i] = element;
        }

        public NamespaceList /*!*/ Clone()
        {
            var elements = this.elements;
            var n = Count;
            var result = new NamespaceList(n);
            result.Count = n;
            var newElements = result.elements;
            for (var i = 0; i < n; i++)
                newElements[i] = elements[i];
            return result;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        public struct Enumerator
        {
            private int index;

            private readonly NamespaceList /*!*/
                list;

            public Enumerator(NamespaceList /*!*/ list)
            {
                index = -1;
                this.list = list;
            }

            public Namespace Current => list[index];

            public bool MoveNext()
            {
                return ++index < list.Count;
            }

            public void Reset()
            {
                index = -1;
            }
        }
    }
#if !FxCop
    public
#endif
        sealed class NodeList
    {
        private Node[] /*!*/
            elements;

        public NodeList()
        {
            elements = new Node[4];
            //^ base();
        }

        public NodeList(int capacity)
        {
            elements = new Node[capacity];
            //^ base();
        }

        public NodeList(params Node[] elements)
        {
            if (elements == null) elements = new Node[0];
            this.elements = elements;
            Count = elements.Length;
            //^ base();
        }

        public int Count { get; private set; }

        [Obsolete("Use Count property instead.")]
        public int Length => Count;

        public Node this[int index]
        {
            get { return elements[index]; }
            set { elements[index] = value; }
        }

        public void Add(Node element)
        {
            var n = elements.Length;
            var i = Count++;
            if (i == n)
            {
                var m = n * 2;
                if (m < 4) m = 4;
                var newElements = new Node[m];
                for (var j = 0; j < n; j++) newElements[j] = elements[j];
                elements = newElements;
            }

            elements[i] = element;
        }

        public NodeList /*!*/ Clone()
        {
            var elements = this.elements;
            var n = Count;
            var result = new NodeList(n);
            result.Count = n;
            var newElements = result.elements;
            for (var i = 0; i < n; i++)
                newElements[i] = elements[i];
            return result;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        public struct Enumerator
        {
            private int index;

            private readonly NodeList /*!*/
                list;

            public Enumerator(NodeList /*!*/ list)
            {
                index = -1;
                this.list = list;
            }

            public Node Current => list[index];

            public bool MoveNext()
            {
                return ++index < list.Count;
            }

            public void Reset()
            {
                index = -1;
            }
        }
    }

    public sealed class ParameterList
    {
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly ParameterList /*!*/
            Empty = new ParameterList(0);

        private Parameter[] /*!*/
            elements;

        public ParameterList()
        {
            elements = new Parameter[8];
            //^ base();
        }

        public ParameterList(int capacity)
        {
            elements = new Parameter[capacity];
            //^ base();
        }

        public ParameterList(params Parameter[] elements)
        {
            if (elements == null) elements = new Parameter[0];
            this.elements = elements;
            Count = elements.Length;
            //^ base();
        }

        public int Count { get; set; }

        [Obsolete("Use Count property instead.")]
        public int Length
        {
            get { return Count; }
            set { Count = value; }
        }

        public Parameter this[int index]
        {
            get { return elements[index]; }
            set { elements[index] = value; }
        }

        public void Add(Parameter element)
        {
            var n = elements.Length;
            var i = Count++;
            if (i == n)
            {
                var m = n * 2;
                if (m < 8) m = 8;
                var newElements = new Parameter[m];
                for (var j = 0; j < n; j++) newElements[j] = elements[j];
                elements = newElements;
            }

            elements[i] = element;
        }

        public ParameterList /*!*/ Clone()
        {
            var elements = this.elements;
            var n = Count;
            var result = new ParameterList(n);
            result.Count = n;
            var newElements = result.elements;
            for (var i = 0; i < n; i++)
                newElements[i] = elements[i];
            return result;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        public override string ToString()
        {
            var res = "";
            for (var i = 0; i < Count; i++)
            {
                if (i > 0) res += ",";
                var par = elements[i];
                if (par == null) continue;
                res += par.ToString();
            }

            return res;
        }

        public struct Enumerator
        {
            private int index;

            private readonly ParameterList /*!*/
                list;

            public Enumerator(ParameterList /*!*/ list)
            {
                index = -1;
                this.list = list;
            }

            public Parameter Current => list[index];

            public bool MoveNext()
            {
                return ++index < list.Count;
            }

            public void Reset()
            {
                index = -1;
            }
        }
    }
#if !NoWriter
    public sealed class PropertyList
    {
        private Property[] /*!*/
            elements;

        public PropertyList()
        {
            elements = new Property[8];
            //^ base();
        }

        public PropertyList(int capacity)
        {
            elements = new Property[capacity];
            //^ base();
        }

        public int Count { get; private set; }

        [Obsolete("Use Count property instead.")]
        public int Length => Count;

        public Property this[int index]
        {
            get { return elements[index]; }
            set { elements[index] = value; }
        }

        public void Add(Property element)
        {
            var n = elements.Length;
            var i = Count++;
            if (i == n)
            {
                var m = n * 2;
                if (m < 8) m = 8;
                var newElements = new Property[m];
                for (var j = 0; j < n; j++) newElements[j] = elements[j];
                elements = newElements;
            }

            elements[i] = element;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        public struct Enumerator
        {
            private int index;

            private readonly PropertyList /*!*/
                list;

            public Enumerator(PropertyList /*!*/ list)
            {
                index = -1;
                this.list = list;
            }

            public Property Current => list[index];

            public bool MoveNext()
            {
                return ++index < list.Count;
            }

            public void Reset()
            {
                index = -1;
            }
        }
    }
#endif
    public sealed class ResourceList
    {
        private Resource[] /*!*/
            elements;

        public ResourceList()
        {
            elements = new Resource[4];
            //^ base();
        }

        public ResourceList(int capacity)
        {
            elements = new Resource[capacity];
            //^ base();
        }

        public int Count { get; private set; }

        [Obsolete("Use Count property instead.")]
        public int Length => Count;

        public Resource this[int index]
        {
            get { return elements[index]; }
            set { elements[index] = value; }
        }

        public void Add(Resource element)
        {
            var n = elements.Length;
            var i = Count++;
            if (i == n)
            {
                var m = n * 2;
                if (m < 4) m = 4;
                var newElements = new Resource[m];
                for (var j = 0; j < n; j++) newElements[j] = elements[j];
                elements = newElements;
            }

            elements[i] = element;
        }

        public ResourceList /*!*/ Clone()
        {
            var elements = this.elements;
            var n = Count;
            var result = new ResourceList(n);
            result.Count = n;
            var newElements = result.elements;
            for (var i = 0; i < n; i++)
                newElements[i] = elements[i];
            return result;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        public struct Enumerator
        {
            private int index;

            private readonly ResourceList /*!*/
                list;

            public Enumerator(ResourceList /*!*/ list)
            {
                index = -1;
                this.list = list;
            }

            public Resource Current => list[index];

            public bool MoveNext()
            {
                return ++index < list.Count;
            }

            public void Reset()
            {
                index = -1;
            }
        }
    }
#if !MinimalReader && !CodeContracts
  public sealed class ScopeList{
    private Scope[]/*!*/ elements;
    private int count = 0;
    public ScopeList(){
      this.elements = new Scope[32];
      //^ base();
    }
    public ScopeList(int capacity){
      this.elements = new Scope[capacity];
      //^ base();
    }
    public ScopeList(params Scope[] elements){
      if (elements == null) elements = new Scope[0];
      this.elements = elements;
      this.count = elements.Length;
      //^ base();
    }
    public void Add(Scope element){
      Scope[] elements = this.elements;
      int n = elements.Length;
      int i = this.count++;
      if (i == n){
        int m = n*2; if (m < 32) m = 32;
        Scope[] newElements = new Scope[m];
        for (int j = 0; j < n; j++) newElements[j] = elements[j];
        this.elements = newElements;
      }
      this.elements[i] = element;
    }
    public ScopeList/*!*/ Clone() {
      Scope[] elements = this.elements;
      int n = this.count;
      ScopeList result = new ScopeList(n);
      result.count = n;
      Scope[] newElements = result.elements;
      for (int i = 0; i < n; i++)
        newElements[i] = elements[i];
      return result;
    }
    public void Insert(Scope element, int index){
      Scope[] elements = this.elements;
      int n = this.elements.Length;
      int i = this.count++;
      if (index >= i) throw new IndexOutOfRangeException();
      if (i == n){
        int m = n*2; if (m < 32) m = 32;
        Scope[] newElements = new Scope[m];
        for (int j = 0; j < index; j++) newElements[j] = elements[j];
        newElements[index] = element;
        for (int j = index; j < n; j++) newElements[j+1] = elements[j];
        return;
      }
      for (int j = index; j < i; j++){
        Scope t = elements[j];
        elements[j] = element;
        element = t;
      }
      elements[i] = element;
    }
    public int Count{
      get{return this.count;}
    }
    [Obsolete("Use Count property instead.")]
    public int Length{
      get{return this.count;}
    }
    public int SearchFor(Scope element){
      Scope[] elements = this.elements;
      for (int i = 0, n = this.count; i < n; i++)
        if ((object)elements[i] == (object)element) return i;
      return -1;
    }
    public Scope this[int index]{
      get{
        return this.elements[index];
      }
      set{
        this.elements[index] = value;
      }
    }
    public Enumerator GetEnumerator(){
      return new Enumerator(this);
    }
    public struct Enumerator{
      private int index;
      private readonly ScopeList/*!*/ list;
      public Enumerator(ScopeList/*!*/ list) {
        this.index = -1;
        this.list = list;
      }
      public Scope Current{
        get{
          return this.list[this.index];
        }
      }
      public bool MoveNext(){
        return ++this.index < this.list.count;
      }
      public void Reset(){
        this.index = -1;
      }
    }
  }
#endif
    public sealed class SecurityAttributeList
    {
        private SecurityAttribute[] /*!*/
            elements;

        public SecurityAttributeList()
        {
            elements = new SecurityAttribute[8];
            //^ base();
        }

        public SecurityAttributeList(int capacity)
        {
            elements = new SecurityAttribute[capacity];
            //^ base();
        }

        public int Count { get; private set; }

        [Obsolete("Use Count property instead.")]
        public int Length => Count;

        public SecurityAttribute this[int index]
        {
            get { return elements[index]; }
            set { elements[index] = value; }
        }

        public void Add(SecurityAttribute element)
        {
            var n = elements.Length;
            var i = Count++;
            if (i == n)
            {
                var m = n * 2;
                if (m < 8) m = 8;
                var newElements = new SecurityAttribute[m];
                for (var j = 0; j < n; j++) newElements[j] = elements[j];
                elements = newElements;
            }

            elements[i] = element;
        }

        public SecurityAttributeList /*!*/ Clone()
        {
            var elements = this.elements;
            var n = Count;
            var result = new SecurityAttributeList(n);
            result.Count = n;
            var newElements = result.elements;
            for (var i = 0; i < n; i++)
                newElements[i] = elements[i];
            return result;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        public struct Enumerator
        {
            private int index;

            private readonly SecurityAttributeList /*!*/
                list;

            public Enumerator(SecurityAttributeList /*!*/ list)
            {
                index = -1;
                this.list = list;
            }

            public SecurityAttribute Current => list[index];

            public bool MoveNext()
            {
                return ++index < list.Count;
            }

            public void Reset()
            {
                index = -1;
            }
        }
    }
#if !MinimalReader
    public sealed class SourceChangeList
    {
        private SourceChange[] /*!*/
            elements;

        public SourceChangeList()
        {
            elements = new SourceChange[4];
            //^ base();
        }

        public SourceChangeList(int capacity)
        {
            elements = new SourceChange[capacity];
            //^ base();
        }

        public SourceChangeList(params SourceChange[] elements)
        {
            if (elements == null) elements = new SourceChange[0];
            this.elements = elements;
            Count = elements.Length;
            //^ base();
        }

        public int Count { get; private set; }

        [Obsolete("Use Count property instead.")]
        public int Length => Count;

        public SourceChange this[int index]
        {
            get { return elements[index]; }
            set { elements[index] = value; }
        }

        public void Add(SourceChange element)
        {
            var n = elements.Length;
            var i = Count++;
            if (i == n)
            {
                var m = n * 2;
                if (m < 4) m = 4;
                var newElements = new SourceChange[m];
                for (var j = 0; j < n; j++) newElements[j] = elements[j];
                elements = newElements;
            }

            elements[i] = element;
        }

        public SourceChangeList /*!*/ Clone()
        {
            var elements = this.elements;
            var n = Count;
            var result = new SourceChangeList(n);
            result.Count = n;
            var newElements = result.elements;
            for (var i = 0; i < n; i++)
                newElements[i] = elements[i];
            return result;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        public struct Enumerator
        {
            private int index;

            private readonly SourceChangeList /*!*/
                list;

            public Enumerator(SourceChangeList /*!*/ list)
            {
                index = -1;
                this.list = list;
            }

            public SourceChange Current => list[index];

            public bool MoveNext()
            {
                return ++index < list.Count;
            }

            public void Reset()
            {
                index = -1;
            }
        }
    }
#endif
    public sealed class StatementList
    {
        private Statement[] /*!*/
            elements;

        public StatementList()
        {
            elements = new Statement[4];
            //^ base();
        }

        public StatementList(int capacity)
        {
            elements = new Statement[capacity];
            //^ base();
        }

        public StatementList(params Statement[] elements)
        {
            if (elements == null) elements = new Statement[0];
            this.elements = elements;
            Length = elements.Length;
            //^ base();
        }

        public int Count
        {
            get
            {
#if CLOUSOT
                Contract.Ensures(Contract.Result<int>() >= 0);
#endif
                return Length;
            }
            set { Length = value; }
        }

        [Obsolete("Use Count property instead.")]
        public int Length { get; set; }

        public Statement this[int index]
        {
            get
            {
#if CLOUSOT
                Contract.Requires(index >= 0);
                Contract.Requires(index < Count);
#endif
                return elements[index];
            }
            set { elements[index] = value; }
        }

        public void Add(Statement statement)
        {
            var n = elements.Length;
            var i = Length++;
            if (i == n)
            {
                var m = n * 2;
                if (m < 32) m = 32;
                var newElements = new Statement[m];
                for (var j = 0; j < n; j++) newElements[j] = elements[j];
                elements = newElements;
            }

            elements[i] = statement;
        }

        public StatementList /*!*/ Clone()
        {
            var elements = this.elements;
            var n = Length;
            var result = new StatementList(n);
            result.Length = n;
            var newElements = result.elements;
            for (var i = 0; i < n; i++)
                newElements[i] = elements[i];
            return result;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        public struct Enumerator
        {
            private int index;

            private readonly StatementList /*!*/
                list;

            public Enumerator(StatementList /*!*/ list)
            {
                index = -1;
                this.list = list;
            }

            public Statement Current => list[index];

            public bool MoveNext()
            {
                return ++index < list.Length;
            }

            public void Reset()
            {
                index = -1;
            }
        }
    }
#if !NoWriter
    public sealed class StringList
    {
        private string[] /*!*/
            elements = new string[4];

        public StringList()
        {
            elements = new string[4];
            //^ base();
        }

        public StringList(int capacity)
        {
            elements = new string[capacity];
            //^ base();
        }

        public StringList(params string[] elements)
        {
            if (elements == null) elements = new string[0];
            this.elements = elements;
            Count = elements.Length;
            //^ base();
        }

        public StringList(StringCollection /*!*/ stringCollection)
        {
            var n = Count = stringCollection == null ? 0 : stringCollection.Count;
            var elements = this.elements = new string[n];
            //^ base();
            if (n > 0) stringCollection.CopyTo(elements, 0);
        }

        public int Count { get; private set; }

        [Obsolete("Use Count property instead.")]
        public int Length => Count;

        public string this[int index]
        {
            get { return elements[index]; }
            set { elements[index] = value; }
        }

        public void Add(string element)
        {
            var n = elements.Length;
            var i = Count++;
            if (i == n)
            {
                var m = n * 2;
                if (m < 4) m = 4;
                var newElements = new string[m];
                for (var j = 0; j < n; j++) newElements[j] = elements[j];
                elements = newElements;
            }

            elements[i] = element;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        public struct Enumerator
        {
            private int index;

            private readonly StringList /*!*/
                list;

            public Enumerator(StringList /*!*/ list)
            {
                index = -1;
                this.list = list;
            }

            public string Current => list[index];

            public bool MoveNext()
            {
                return ++index < list.Count;
            }

            public void Reset()
            {
                index = -1;
            }
        }
    }
#endif
#if !MinimalReader && !CodeContracts
  public sealed class SwitchCaseList{
    private SwitchCase[]/*!*/ elements = new SwitchCase[16];
    private int count = 0;
    public SwitchCaseList(){
      this.elements = new SwitchCase[16];
      //^ base();
    }
    public SwitchCaseList(int capacity){
      this.elements = new SwitchCase[capacity];
      //^ base();
    }
    public void Add(SwitchCase element){
      int n = this.elements.Length;
      int i = this.count++;
      if (i == n){
        int m = n*2; if (m < 16) m = 16;
        SwitchCase[] newElements = new SwitchCase[m];
        for (int j = 0; j < n; j++) newElements[j] = elements[j];
        this.elements = newElements;
      }
      this.elements[i] = element;
    }
    public SwitchCaseList/*!*/ Clone() {
      SwitchCase[] elements = this.elements;
      int n = this.count;
      SwitchCaseList result = new SwitchCaseList(n);
      result.count = n;
      SwitchCase[] newElements = result.elements;
      for (int i = 0; i < n; i++)
        newElements[i] = elements[i];
      return result;
    }
    public int Count{
      get{return this.count;}
    }
    [Obsolete("Use Count property instead.")]
    public int Length{
      get{return this.count;}
    }
    public SwitchCase this[int index]{
      get{
        return this.elements[index];
      }
      set{
        this.elements[index] = value;
      }
    }
    public Enumerator GetEnumerator(){
      return new Enumerator(this);
    }
    public struct Enumerator{
      private int index;
      private readonly SwitchCaseList/*!*/ list;
      public Enumerator(SwitchCaseList/*!*/ list) {
        this.index = -1;
        this.list = list;
      }
      public SwitchCase Current{
        get{
          return this.list[this.index];
        }
      }
      public bool MoveNext(){
        return ++this.index < this.list.count;
      }
      public void Reset(){
        this.index = -1;
      }
    }
  }
#endif
    public sealed class TypeNodeList
    {
        private TypeNode[] /*!*/
            elements;

        public TypeNodeList()
        {
            elements = new TypeNode[32];
            //^ base();
        }

        public TypeNodeList(int capacity)
        {
            elements = new TypeNode[capacity];
            //^ base();
        }

        public TypeNodeList(params TypeNode[] elements)
        {
            if (elements == null) elements = new TypeNode[0];
            this.elements = elements;
            Count = elements.Length;
            //^ base();
        }

        public int Count { get; private set; }

        [Obsolete("Use Count property instead.")]
        public int Length => Count;

        public TypeNode this[int index]
        {
            get { return elements[index]; }
            set { elements[index] = value; }
        }

        public void Add(TypeNode element)
        {
            var elements = this.elements;
            var n = elements.Length;
            var i = Count++;
            if (i == n)
            {
                var m = n * 2;
                if (m < 32) m = 32;
                var newElements = new TypeNode[m];
                for (var j = 0; j < n; j++) newElements[j] = elements[j];
                this.elements = newElements;
            }

            this.elements[i] = element;
        }

        public TypeNodeList /*!*/ Clone()
        {
            var elements = this.elements;
            var n = Count;
            var result = new TypeNodeList(n);
            result.Count = n;
            var newElements = result.elements;
            for (var i = 0; i < n; i++)
                newElements[i] = elements[i];
            return result;
        }

        public void Insert(TypeNode element, int index)
        {
            var elements = this.elements;
            var n = this.elements.Length;
            var i = Count++;
            if (index >= i) throw new IndexOutOfRangeException();
            if (i == n)
            {
                var m = n * 2;
                if (m < 32) m = 32;
                var newElements = new TypeNode[m];
                for (var j = 0; j < index; j++) newElements[j] = elements[j];
                newElements[index] = element;
                for (var j = index; j < n; j++) newElements[j + 1] = elements[j];
                return;
            }

            for (var j = index; j < i; j++)
            {
                var t = elements[j];
                elements[j] = element;
                element = t;
            }

            elements[i] = element;
        }

        public int SearchFor(TypeNode element)
        {
            var elements = this.elements;
            for (int i = 0, n = Count; i < n; i++)
                if (elements[i] == (object)element)
                    return i;
            return -1;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        internal bool Contains(TypeNode asType)
        {
            return SearchFor(asType) >= 0;
        }

        public struct Enumerator
        {
            private int index;

            private readonly TypeNodeList /*!*/
                list;

            public Enumerator(TypeNodeList /*!*/ list)
            {
                index = -1;
                this.list = list;
            }

            public TypeNode Current => list[index];

            public bool MoveNext()
            {
                return ++index < list.Count;
            }

            public void Reset()
            {
                index = -1;
            }
        }
    }
#if !MinimalReader && !CodeContracts
  public sealed class TypeswitchCaseList{
    private TypeswitchCase[]/*!*/ elements = new TypeswitchCase[16];
    private int count = 0;
    public TypeswitchCaseList(){
      this.elements = new TypeswitchCase[16];
      //^ base();
    }
    public TypeswitchCaseList(int capacity){
      this.elements = new TypeswitchCase[capacity];
      //^ base();
    }
    public void Add(TypeswitchCase element){
      int n = this.elements.Length;
      int i = this.count++;
      if (i == n){
        int m = n*2; if (m < 16) m = 16;
        TypeswitchCase[] newElements = new TypeswitchCase[m];
        for (int j = 0; j < n; j++) newElements[j] = elements[j];
        this.elements = newElements;
      }
      this.elements[i] = element;
    }
    public TypeswitchCaseList/*!*/ Clone() {
      TypeswitchCase[] elements = this.elements;
      int n = this.count;
      TypeswitchCaseList result = new TypeswitchCaseList(n);
      result.count = n;
      TypeswitchCase[] newElements = result.elements;
      for (int i = 0; i < n; i++)
        newElements[i] = elements[i];
      return result;
    }
    public int Count{
      get{return this.count;}
    }
    [Obsolete("Use Count property instead.")]
    public int Length{
      get{return this.count;}
    }
    public TypeswitchCase this[int index]{
      get{
        return this.elements[index];
      }
      set{
        this.elements[index] = value;
      }
    }
    public Enumerator GetEnumerator(){
      return new Enumerator(this);
    }
    public struct Enumerator{
      private int index;
      private readonly TypeswitchCaseList/*!*/ list;
      public Enumerator(TypeswitchCaseList/*!*/ list) {
        this.index = -1;
        this.list = list;
      }
      public TypeswitchCase Current{
        get{
          return this.list[this.index];
        }
      }
      public bool MoveNext(){
        return ++this.index < this.list.count;
      }
      public void Reset(){
        this.index = -1;
      }
    }
  }
  public sealed class UsedNamespaceList{
    private UsedNamespace[]/*!*/ elements;
    private int count = 0;
    public UsedNamespaceList(){
      this.elements = new UsedNamespace[4];
      //^ base();
    }
    public UsedNamespaceList(int capacity){
      this.elements = new UsedNamespace[capacity];
      //^ base();
    }
    public void Add(UsedNamespace element){
      int n = this.elements.Length;
      int i = this.count++;
      if (i == n){
        int m = n*2; if (m < 4) m = 4;
        UsedNamespace[] newElements = new UsedNamespace[m];
        for (int j = 0; j < n; j++) newElements[j] = elements[j];
        this.elements = newElements;
      }
      this.elements[i] = element;
    }
    public UsedNamespaceList/*!*/ Clone() {
      UsedNamespace[] elements = this.elements;
      int n = this.count;
      UsedNamespaceList result = new UsedNamespaceList(n);
      result.count = n;
      UsedNamespace[] newElements = result.elements;
      for (int i = 0; i < n; i++)
        newElements[i] = elements[i];
      return result;
    }
    public int Count{
      get{return this.count;}
    }
    [Obsolete("Use Count property instead.")]
    public int Length{
      get{return this.count;}
    }
    public UsedNamespace this[int index]{
      get{
        return this.elements[index];
      }
      set{
        this.elements[index] = value;
      }
    }
    public Enumerator GetEnumerator(){
      return new Enumerator(this);
    }
    public struct Enumerator{
      private int index;
      private readonly UsedNamespaceList/*!*/ list;
      public Enumerator(UsedNamespaceList/*!*/ list) {
        this.index = -1;
        this.list = list;
      }
      public UsedNamespace Current{
        get{
          return this.list[this.index];
        }
      }
      public bool MoveNext(){
        return ++this.index < this.list.count;
      }
      public void Reset(){
        this.index = -1;
      }
    }
  }

  public sealed class VariableDeclarationList{
    private VariableDeclaration[]/*!*/ elements;
    private int count = 0;
    public VariableDeclarationList(){
      this.elements = new VariableDeclaration[4];
      //^ base();
    }
    public VariableDeclarationList(int capacity){
      this.elements = new VariableDeclaration[capacity];
      //^ base();
    }
    public void Add(VariableDeclaration element){
      int n = this.elements.Length;
      int i = this.count++;
      if (i == n){
        int m = n*2; if (m < 4) m = 4;
        VariableDeclaration[] newElements = new VariableDeclaration[m];
        for (int j = 0; j < n; j++) newElements[j] = elements[j];
        this.elements = newElements;
      }
      this.elements[i] = element;
    }
    public int Count{
      get{return this.count;}
    }
    [Obsolete("Use Count property instead.")]
    public int Length{
      get{return this.count;}
    }
    public VariableDeclaration this[int index]{
      get{
        return this.elements[index];
      }
      set{
        this.elements[index] = value;
      }
    }
    public Enumerator GetEnumerator(){
      return new Enumerator(this);
    }
    public struct Enumerator{
      private int index;
      private readonly VariableDeclarationList/*!*/ list;
      public Enumerator(VariableDeclarationList/*!*/ list) {
        this.index = -1;
        this.list = list;
      }
      public VariableDeclaration Current{
        get{
          return this.list[this.index];
        }
      }
      public bool MoveNext(){
        return ++this.index < this.list.count;
      }
      public void Reset(){
        this.index = -1;
      }
    }
  }
#endif
    public sealed class Win32ResourceList
    {
        private Win32Resource[] /*!*/
            elements;

        public Win32ResourceList()
        {
            elements = new Win32Resource[4];
            //^ base();
        }

        public Win32ResourceList(int capacity)
        {
            elements = new Win32Resource[capacity];
            //^ base();
        }

        public int Count { get; private set; }

        [Obsolete("Use Count property instead.")]
        public int Length => Count;

        public Win32Resource this[int index]
        {
            get { return elements[index]; }
            set { elements[index] = value; }
        }

        public void Add(Win32Resource element)
        {
            var n = elements.Length;
            var i = Count++;
            if (i == n)
            {
                var m = n * 2;
                if (m < 4) m = 4;
                var newElements = new Win32Resource[m];
                for (var j = 0; j < n; j++) newElements[j] = elements[j];
                elements = newElements;
            }

            elements[i] = element;
        }

        public Win32ResourceList /*!*/ Clone()
        {
            var elements = this.elements;
            var n = Count;
            var result = new Win32ResourceList(n);
            result.Count = n;
            var newElements = result.elements;
            for (var i = 0; i < n; i++)
                newElements[i] = elements[i];
            return result;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        public struct Enumerator
        {
            private int index;

            private readonly Win32ResourceList /*!*/
                list;

            public Enumerator(Win32ResourceList /*!*/ list)
            {
                index = -1;
                this.list = list;
            }

            public Win32Resource Current => list[index];

            public bool MoveNext()
            {
                return ++index < list.Count;
            }

            public void Reset()
            {
                index = -1;
            }
        }
    }
}
#else
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.Cci
{
  public abstract class MetadataCollection<T> : IList<T>, IList{
    private List<T> innerList;
    internal MetadataCollection() : this(new List<T>()){}
    internal MetadataCollection(int capacity) : this(new List<T>(capacity)){}
    internal MetadataCollection(List<T> innerList){
      this.innerList = innerList;
    }
    internal MetadataCollection(ICollection<T> collection) : this(collection == null ? 0 : collection.Count){
      if (collection != null){
        this.innerList.AddRange(collection);
      }
    }
    public T this[int index]{
      get { return this.innerList[index]; }
      internal set { this.innerList[index] = value; }
    }
    public int IndexOf(T item){
      return this.innerList.IndexOf(item);
    }
    public bool Contains(T item){
      return this.innerList.Contains(item);
    }
    public void CopyTo(T[] array, int arrayIndex){
      this.innerList.CopyTo(array, arrayIndex);
    }
    public int Count{
      get { return this.innerList.Count; }
    }
    public bool IsReadOnly{
      get { return true; }
    }
    public Enumerator GetEnumerator(){
      return new Enumerator(this.innerList.GetEnumerator());
    }
    public struct Enumerator{
      private List<T>.Enumerator enumerator;
      public Enumerator(List<T>.Enumerator enumerator){
        this.enumerator = enumerator;
      }
      public bool MoveNext(){
        return this.enumerator.MoveNext();
      }
      public T Current{
        get { return this.enumerator.Current; }
      }
    }
    internal void Add(T item){
      this.innerList.Add(item);
    }
    void ICollection<T>.Add(T item){
      throw new NotSupportedException(ExceptionStrings.CollectionIsReadOnly);
    }
    void ICollection<T>.Clear(){
      throw new NotSupportedException(ExceptionStrings.CollectionIsReadOnly);
    }
    bool ICollection<T>.Remove(T item){
      throw new NotSupportedException(ExceptionStrings.CollectionIsReadOnly);
    }
    IEnumerator<T> IEnumerable<T>.GetEnumerator(){
      return this.innerList.GetEnumerator();
    }
    void IList<T>.Insert(int index, T item){
      throw new NotSupportedException(ExceptionStrings.CollectionIsReadOnly);
    }
    void IList<T>.RemoveAt(int index){
      throw new NotSupportedException(ExceptionStrings.CollectionIsReadOnly);
    }
    T IList<T>.this[int index]{
      get { return this[index]; }
      set { throw new NotSupportedException(ExceptionStrings.CollectionIsReadOnly); }
    }
    void ICollection.CopyTo(Array array, int index){
      ICollection list = this.innerList;
      list.CopyTo(array, index);
    }
    bool ICollection.IsSynchronized{
      get { return false; }
    }
    object ICollection.SyncRoot{
      get {
        ICollection list = this.innerList;
        return list.SyncRoot; 
      }
    }
    IEnumerator IEnumerable.GetEnumerator(){
      IEnumerable list = this.innerList;
      return list.GetEnumerator();
    }
    int IList.Add(object value){
      throw new NotSupportedException(ExceptionStrings.CollectionIsReadOnly);
    }
    bool IList.Contains(object value){
      IList list = this.innerList;
      return list.Contains(value);
    }
    int IList.IndexOf(object value){
      IList list = this.innerList;
      return list.IndexOf(value);
    }
    void IList.Insert(int index, object value){
      throw new NotSupportedException(ExceptionStrings.CollectionIsReadOnly);
    }
    bool IList.IsFixedSize{
      get { return true; }
    }
    void IList.Remove(object value){
      throw new NotSupportedException(ExceptionStrings.CollectionIsReadOnly);
    }
    void IList.RemoveAt(int index){
      throw new NotSupportedException(ExceptionStrings.CollectionIsReadOnly);
    }
    object IList.this[int index]{
      get { return this[index]; }
      set { throw new NotSupportedException(ExceptionStrings.CollectionIsReadOnly); }
    }
    void IList.Clear(){
      throw new NotSupportedException(ExceptionStrings.CollectionIsReadOnly);
    }
  }
  public sealed class AssemblyNodeCollection : MetadataCollection<AssemblyNode> {
    internal AssemblyNodeCollection() : base(){}
    internal AssemblyNodeCollection(int capacity) : base(capacity) { }
    internal AssemblyNodeCollection(ICollection<AssemblyNode> collection) : base(collection) { }
    internal AssemblyNodeCollection Clone() {
      return new AssemblyNodeCollection(this);
    }
  }
  public sealed class AssemblyReferenceCollection : MetadataCollection<AssemblyReference> {
    internal AssemblyReferenceCollection() : base(){}
    internal AssemblyReferenceCollection(int capacity) : base(capacity){}
  }
  public sealed class AttributeNodeCollection : MetadataCollection<AttributeNode> {
    internal AttributeNodeCollection() : base(){}
    internal AttributeNodeCollection(int capacity) : base(capacity){}
    internal AttributeNodeCollection(ICollection<AttributeNode> collection) : base(collection) { }
    internal AttributeNodeCollection Clone() {
      return new AttributeNodeCollection(this);
    }
  }
  public sealed class BlockCollection : MetadataCollection<Block> {
    internal BlockCollection() : base(){}
    internal BlockCollection(int capacity) : base(capacity) { }
    internal BlockCollection(ICollection<Block> collection) : base(collection) { }
    internal BlockCollection Clone() {
      return new BlockCollection(this);
    }
  }
  public sealed class CatchNodeCollection : MetadataCollection<CatchNode> {
    internal CatchNodeCollection() : base(){}
    internal CatchNodeCollection(int capacity) : base(capacity) { }
  }
  public sealed class ExpressionCollection : MetadataCollection<Expression> {
    internal ExpressionCollection() : base(){}
    internal ExpressionCollection(int capacity) : base(capacity){}
    internal ExpressionCollection(ICollection<Expression> collection) : base(collection) { }
    internal ExpressionCollection Clone() {
      return new ExpressionCollection(this);
    }
  }
  public sealed class InstructionCollection : MetadataCollection<Instruction> {
    internal InstructionCollection() : base(){}
    internal InstructionCollection(int capacity) : base(capacity){}
  }
  public sealed class InterfaceCollection : MetadataCollection<InterfaceNode> {
    internal InterfaceCollection() : base(){}
    internal InterfaceCollection(int capacity) : base(capacity){}
    internal InterfaceCollection(params InterfaceNode[] range) : base(range) {}   
    internal InterfaceCollection(ICollection<InterfaceNode> collection) : base(collection) { }
    internal InterfaceCollection Clone() {
      return new InterfaceCollection(this);
    }
  }
  public sealed class LocalCollection : MetadataCollection<Local> {
    internal LocalCollection() : base(){}
    internal LocalCollection(int capacity) : base(capacity){}
  }
  public sealed class MemberCollection : MetadataCollection<Member> {
    internal MemberCollection() : base(){}
    internal MemberCollection(int capacity) : base(capacity){}
    internal MemberCollection(ICollection<Member> collection) : base(collection) { }
    internal MemberCollection Clone() {
      return new MemberCollection(this);
    }
  }
  public sealed class MethodCollection : MetadataCollection<Method> {
    internal MethodCollection() : base(){}
    internal MethodCollection(int capacity) : base(capacity){}
    internal MethodCollection(params Method[] range) : base(range) {}
  }
  public sealed class ModuleReferenceCollection : MetadataCollection<ModuleReference> {
    internal ModuleReferenceCollection() : base(){}
    internal ModuleReferenceCollection(int capacity) : base(capacity){}
  }
  public sealed class NamespaceCollection : MetadataCollection<Namespace> {
    internal NamespaceCollection() : base(){}
    internal NamespaceCollection(int capacity) : base(capacity){}
  }
  public sealed class NodeCollection : MetadataCollection<Node> {
    internal NodeCollection() : base(){}
    internal NodeCollection(int capacity) : base(capacity){}
    internal NodeCollection(ICollection<Node> collection) : base(collection) { }
    internal NodeCollection Clone() {
      return new NodeCollection(this);
    }
  }
  public sealed class ParameterCollection : MetadataCollection<Parameter> {
    internal ParameterCollection() : base(){}
    internal ParameterCollection(int capacity) : base(capacity){}
    internal ParameterCollection(ICollection<Parameter> collection) : base(collection) { }
    internal ParameterCollection Clone() {
      return new ParameterCollection(this);
    }
  }
  public sealed class ResourceCollection : MetadataCollection<Resource> {
    internal ResourceCollection() : base(){}
    internal ResourceCollection(int capacity) : base(capacity){}
  }
  public sealed class SecurityAttributeCollection : MetadataCollection<SecurityAttribute> {
    internal SecurityAttributeCollection() : base(){}
    internal SecurityAttributeCollection(int capacity) : base(capacity){}
    internal SecurityAttributeCollection(ICollection<SecurityAttribute> collection) : base(collection) { }
    internal SecurityAttributeCollection Clone() {
      return new SecurityAttributeCollection(this);
    }
  }
  public sealed class StackVariableCollection : MetadataCollection<Local> {
    internal StackVariableCollection() : base(){}
    internal StackVariableCollection(int capacity) : base(capacity) { }
  }
  public sealed class StatementCollection : MetadataCollection<Statement> {
    internal StatementCollection() : base(){}
    internal StatementCollection(int capacity) : base(capacity){}
    internal StatementCollection(ICollection<Statement> collection) : base(collection) { }
    internal StatementCollection Clone() {
      return new StatementCollection(this);
    }
  }
  public sealed class TypeNodeCollection : MetadataCollection<TypeNode> {
    internal TypeNodeCollection() : base(){}
    internal TypeNodeCollection(int capacity) : base(capacity){}
    internal TypeNodeCollection(params TypeNode[] range) : base(range) {}
    internal TypeNodeCollection(ICollection<TypeNode> collection) : base(collection) { }
    internal TypeNodeCollection Clone() {
      return new TypeNodeCollection(this);
    }
  }
  public sealed class Win32ResourceCollection : MetadataCollection<Win32Resource> {
    internal Win32ResourceCollection() : base(){}
    internal Win32ResourceCollection(int capacity) : base(capacity) { }
  }
}
#endif