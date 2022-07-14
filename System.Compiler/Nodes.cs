// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information. 

#if FxCop
using AssemblyReferenceList = Microsoft.Cci.AssemblyReferenceCollection;
using AttributeList = Microsoft.Cci.AttributeNodeCollection;
using BlockList = Microsoft.Cci.BlockCollection;
using ExpressionList = Microsoft.Cci.ExpressionCollection;
using InstructionList = Microsoft.Cci.InstructionCollection;
using Int32List = System.Collections.Generic.List<int>;
using InterfaceList = Microsoft.Cci.InterfaceCollection;
using MemberList = Microsoft.Cci.MemberCollection;
using MethodList = Microsoft.Cci.MethodCollection;
using ModuleReferenceList = Microsoft.Cci.ModuleReferenceCollection;
using NamespaceList = Microsoft.Cci.NamespaceCollection;
using ParameterList = Microsoft.Cci.ParameterCollection;
using ResourceList = Microsoft.Cci.ResourceCollection;
using SecurityAttributeList = Microsoft.Cci.SecurityAttributeCollection;
using StatementList = Microsoft.Cci.StatementCollection;
using TypeNodeList = Microsoft.Cci.TypeNodeCollection;
using Win32ResourceList = Microsoft.Cci.Win32ResourceCollection;
using Module = Microsoft.Cci.ModuleNode;
using Class = Microsoft.Cci.ClassNode;
using Interface = Microsoft.Cci.InterfaceNode;
using Property = Microsoft.Cci.PropertyNode;
using Event = Microsoft.Cci.EventNode;
using Return = Microsoft.Cci.ReturnNode;
using Throw = Microsoft.Cci.ThrowNode;
#endif
#if CCINamespace
using Cci = Microsoft.Cci;
using Microsoft.Cci.Metadata;
using Metadata = Microsoft.Cci.Metadata;
#else
using Cci = System.Compiler;
using System.Compiler.Metadata;
#endif
#if !NoXml
using System.Xml;
#endif
#if CLOUSOT || CodeContracts
using System.Diagnostics.Contracts;
using CC = System.Diagnostics.Contracts;
using Microsoft.Cci.Pdb;
#endif
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Configuration.Assemblies;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Security.Permissions;
using System.Security.Policy;
using System.Text;
using System.Threading;

#if CCINamespace
namespace Microsoft.Cci{
#else
namespace System.Compiler
{
#endif
#if !FxCop
    /// <summary>
    ///     This interface can be used to link an arbitrary source text provider into an IR tree via a DocumentText instance.
    /// </summary>
    public interface ISourceText
    {
        /// <summary>
        ///     The number of characters in the source text.
        ///     A "character" corresponds to a System.Char which is actually a Unicode UTF16 code point to be precise.
        /// </summary>
        int Length { get; }

        /// <summary>
        ///     Retrieves the character at the given position. The first character is at position zero.
        /// </summary>
        char this[int position] { get; }

        /// <summary>
        ///     Retrieves a substring from this instance. The substring starts with the character at the specified index and has a
        ///     specified length.
        /// </summary>
        string Substring(int startIndex, int length);

        /// <summary>
        ///     Indicates that the text has been fully scanned and futher references to the text are expected to be infrequent.
        ///     The underlying object can now choose to clear cached information if it comes under resource pressure.
        /// </summary>
        void MakeCollectible();
    }

    public unsafe interface ISourceTextBuffer : ISourceText
    {
        /// <summary>
        ///     Returns null unless the implementer is based on an ASCII buffer that stays alive as long at the implementer itself.
        ///     An implementer that returns a non-null value is merely a wrapper to keep the buffer alive. No further methods will
        ///     be called on the interface in this case.
        /// </summary>
        byte* Buffer { get; }
    }
#endif
#if !MinimalReader
    /// <summary>
    ///     Use this after a source text has already been scanned and parsed. This allows the source text to get released
    ///     if there is memory pressure, while still allowing portions of it to be retrieved on demand. This is useful when
    ///     a large number of source files are read in, but only infrequent references are made to them.
    /// </summary>
    public sealed class CollectibleSourceText : ISourceText
    {
        private readonly WeakReference /*!*/
            fileContent;

        private readonly string /*!*/
            filePath;

        private int length;

        public CollectibleSourceText(string /*!*/ filePath, int length)
        {
            this.filePath = filePath;
            fileContent = new WeakReference(null);
            this.length = length;
            //^ base();
        }

        public CollectibleSourceText(string /*!*/ filePath, string fileContent)
        {
            this.filePath = filePath;
            this.fileContent = new WeakReference(fileContent);
            length = fileContent == null ? 0 : fileContent.Length;
            //^ base();
        }

        int ISourceText.Length => length;

        string ISourceText.Substring(int startIndex, int length)
        {
            return GetSourceText().Substring(startIndex, length);
        }

        char ISourceText.this[int index] => GetSourceText()[index];

        void ISourceText.MakeCollectible()
        {
            fileContent.Target = null;
        }

        private string /*!*/ ReadFile()
        {
            var content = string.Empty;
            try
            {
                var sr = new StreamReader(filePath);
                content = sr.ReadToEnd();
                length = content.Length;
                sr.Close();
            }
            catch
            {
            }

            return content;
        }

        public string /*!*/ GetSourceText()
        {
            var source = (string)fileContent.Target;
            if (source != null) return source;
            source = ReadFile();
            fileContent.Target = source;
            return source;
        }
    }

    /// <summary>
    ///     This class is used to wrap the string contents of a source file with an ISourceText interface. It is used while
    ///     compiling
    ///     a project the first time in order to obtain a symbol table. After that the StringSourceText instance is typically
    ///     replaced with
    ///     a CollectibleSourceText instance, so that the actual source text string can be collected. When a file is edited,
    ///     and the editor does not provide its own ISourceText wrapper for its edit buffer, this class can be used to wrap a
    ///     copy of the edit buffer.
    /// </summary>
    public sealed class StringSourceText : ISourceText
    {
        /// <summary>
        ///     The wrapped string used to implement ISourceText. Use this value when unwrapping.
        /// </summary>
        public readonly string /*!*/
            SourceText;

        /// <summary>
        ///     True when the wrapped string is the contents of a file. Typically used to check if it safe to replace this
        ///     StringSourceText instance with a CollectibleSourceText instance.
        /// </summary>
        public bool IsSameAsFileContents;

        public StringSourceText(string /*!*/ sourceText, bool isSameAsFileContents)
        {
            SourceText = sourceText;
            IsSameAsFileContents = isSameAsFileContents;
            //^ base();
        }

        int ISourceText.Length => SourceText.Length;

        string ISourceText.Substring(int startIndex, int length)
        {
            return SourceText.Substring(startIndex, length);
        }

        char ISourceText.this[int index] => SourceText[index];

        void ISourceText.MakeCollectible()
        {
        }
    }
#endif

#if !FxCop
    /// <summary>
    ///     This class provides a uniform interface to program sources provided in the form of Unicode strings,
    ///     unsafe pointers to ascii buffers (as obtained from a memory mapped file, for instance) as well as
    ///     arbitrary source text providers that implement the ISourceText interface.
    /// </summary>
    public sealed unsafe class DocumentText
    {
        /// <summary>
        ///     If this is not null it is used to obtain 8-bit ASCII characters.
        /// </summary>
        public byte* AsciiStringPtr;

        /// <summary>
        ///     The number of characters in the source document.
        ///     A "character" corresponds to a System.Char which is actually a Unicode UTF16 code point to be precise.
        /// </summary>
        public int Length;

        /// <summary>
        ///     If this is not null it represents a Unicode string encoded as UTF16.
        /// </summary>
        public string Source;

        /// <summary>
        ///     If this is not null the object implement ISourceText provides some way to get at individual characters and
        ///     substrings.
        /// </summary>
        public ISourceText TextProvider;

        public DocumentText(string source)
        {
            if (source == null)
            {
                Debug.Assert(false);
                return;
            }

            Source = source;
            Length = source.Length;
        }

        public DocumentText(ISourceText textProvider)
        {
            if (textProvider == null)
            {
                Debug.Assert(false);
                return;
            }

            TextProvider = textProvider;
            Length = textProvider.Length;
        }

        public DocumentText(ISourceTextBuffer textProvider)
        {
            if (textProvider == null)
            {
                Debug.Assert(false);
                return;
            }

            TextProvider = textProvider;
            AsciiStringPtr = textProvider.Buffer;
            Length = textProvider.Length;
        }

        /// <summary>
        ///     Retrieves the character at the given position. The first character is at position zero.
        /// </summary>
        public char this[int position]
        {
            get
            {
                if (position < 0 || position >= Length)
                {
                    Debug.Assert(false);
                    return (char)0;
                }

                if (AsciiStringPtr != null) return (char)*(AsciiStringPtr + position);
                if (Source != null) return Source[position];

                if (TextProvider != null) return TextProvider[position];

                Debug.Assert(false);
                return (char)0;
            }
        }

        /// <summary>
        ///     Compare this.Substring(offset, length) for equality with str.
        ///     Call this only if str.Length is known to be equal to length.
        /// </summary>
        public bool Equals(string str, int position, int length)
        {
            //TODO: (int position, int length, string str)
            if (str == null)
            {
                Debug.Assert(false);
                return false;
            }

            if (str.Length != length)
            {
                Debug.Assert(false);
                return false;
            }

            if (position < 0 || position + length > Length)
            {
                Debug.Assert(false);
                return false;
            }

            var p = AsciiStringPtr;
            if (p != null)
            {
                for (int i = position, j = 0; j < length; i++, j++)
                    if ((char)*(p + i) != str[j])
                        return false;
                return true;
            }

            var source = Source;
            if (source != null)
            {
                for (int i = position, j = 0; j < length; i++, j++)
                    if (source[i] != str[j])
                        return false;
                return true;
            }

            var myProvider = TextProvider;
            if (myProvider == null)
            {
                Debug.Assert(false);
                return false;
            }

            for (int i = position, j = 0; j < length; i++, j++)
                if (myProvider[i] != str[j])
                    return false;
            return true;
        }

        /// <summary>
        ///     Compares the substring of the specificied length starting at offset, with the substring in DocumentText starting at
        ///     textOffset.
        /// </summary>
        /// <param name="offset">The index of the first character of the substring of this DocumentText.</param>
        /// <param name="text">The Document text with the substring being compared to.</param>
        /// <param name="textOffset">The index of the first character of the substring of the DocumentText being compared to.</param>
        /// <param name="length">The number of characters in the substring being compared.</param>
        /// <returns></returns>
        public bool Equals(int offset, DocumentText text, int textOffset, int length)
        {
            //TODO: (int position, int length, DocumentText text, int textPosition)
            if (offset < 0 || length < 0 || offset + length > Length)
            {
                Debug.Assert(false);
                return false;
            }

            if (textOffset < 0 || text == null || textOffset + length > text.Length)
            {
                Debug.Assert(false);
                return false;
            }

            var p = AsciiStringPtr;
            if (p != null)
            {
                var q = text.AsciiStringPtr;
                if (q != null)
                {
                    for (int i = offset, j = textOffset, n = offset + length; i < n; i++, j++)
                        if (*(p + i) != *(q + j))
                            return false;
                    return true;
                }

                var textSource = text.Source;
                if (textSource != null)
                {
                    for (int i = offset, j = textOffset, n = offset + length; i < n; i++, j++)
                        if ((char)*(p + i) != textSource[j])
                            return false;
                    return true;
                }

                var textProvider = text.TextProvider;
                if (textProvider == null)
                {
                    Debug.Assert(false);
                    return false;
                }

                for (int i = offset, j = textOffset, n = offset + length; i < n; i++, j++)
                    if ((char)*(p + i) != textProvider[j])
                        return false;
                return true;
            }

            var source = Source;
            if (source != null)
            {
                var q = text.AsciiStringPtr;
                if (q != null)
                {
                    for (int i = offset, j = textOffset, n = offset + length; i < n; i++, j++)
                        if (source[i] != (char)*(q + j))
                            return false;
                    return true;
                }

                var textSource = text.Source;
                if (textSource != null)
                {
                    for (int i = offset, j = textOffset, n = offset + length; i < n; i++, j++)
                        if (source[i] != textSource[j])
                            return false;
                    return true;
                }

                var textProvider = text.TextProvider;
                if (textProvider == null)
                {
                    Debug.Assert(false);
                    return false;
                }

                for (int i = offset, j = textOffset, n = offset + length; i < n; i++, j++)
                    if (source[i] != textProvider[j])
                        return false;
                return true;
            }

            {
                var myProvider = TextProvider;
                if (myProvider == null)
                {
                    Debug.Assert(false);
                    return false;
                }

                var q = text.AsciiStringPtr;
                if (q != null)
                {
                    for (int i = offset, j = textOffset, n = offset + length; i < n; i++, j++)
                        if (myProvider[i] != (char)*(q + j))
                            return false;
                    return true;
                }

                var textSource = text.Source;
                if (textSource != null)
                {
                    for (int i = offset, j = textOffset, n = offset + length; i < n; i++, j++)
                        if (myProvider[i] != textSource[j])
                            return false;
                    return true;
                }

                var textProvider = text.TextProvider;
                if (textProvider == null)
                {
                    Debug.Assert(false);
                    return false;
                }

                for (int i = offset, j = textOffset, n = offset + length; i < n; i++, j++)
                    if (myProvider[i] != textProvider[j])
                        return false;
                return true;
            }
        }

        /// <summary>
        ///     Retrieves a substring from this instance. The substring starts at a specified character position and has a
        ///     specified length.
        /// </summary>
        public string /*!*/ Substring(int position, int length)
        {
            if (position < 0 || length < 0 || position + length > Length + 1)
            {
                Debug.Assert(false);
                return "";
            }

            if (position + length > Length)
                length = Length - position; //Allow virtual EOF character to be included in length
            if (AsciiStringPtr != null) return new string((sbyte*)AsciiStringPtr, position, length, Encoding.ASCII);
            if (Source != null) return Source.Substring(position, length);

            if (TextProvider != null) return TextProvider.Substring(position, length);

            Debug.Assert(false);
            return "";
        }
    }
#endif
#if FxCop
    public class Document{
    internal string Name;
  }
#endif
#if false
  public class Document {
    /// <summary>
    /// A Guid that identifies the kind of document to applications such as a debugger. Typically System.Diagnostics.SymbolStore.SymDocumentType.Text.
    /// </summary>
    public System.Guid DocumentType;
    /// <summary>
    /// A Guid that identifies the programming language used in the source document. Typically used by a debugger to locate language specific logic.
    /// </summary>
    public System.Guid Language;
    /// <summary>
    /// A Guid that identifies the compiler vendor programming language used in the source document. Typically used by a debugger to locate vendor specific logic.
    /// </summary>
    public System.Guid LanguageVendor;
    /// <summary>
    /// The name of the document. Typically a file name. Can be a full or relative file path, or a URI or some other kind of identifier.
    /// </summary>
    public string/*!*/ Name;

  }
#endif
#if !FxCop
    /// <summary>
    ///     A source document from which an Abstract Syntax Tree has been derived.
    /// </summary>
    public class Document
    {
        /// <summary> Add one to this every time a Document instance gets a unique key.</summary>
        private static int uniqueKeyCounter;

        /// <summary>
        ///     A Guid that identifies the kind of document to applications such as a debugger. Typically
        ///     System.Diagnostics.SymbolStore.SymDocumentType.Text.
        /// </summary>
        public Guid DocumentType;

        /// <summary>
        ///     Indicates that the document contains machine generated source code that should not show up in tools such as
        ///     debuggers.
        ///     Can be set by C# preprocessor directives.
        /// </summary>
        public bool Hidden;

        /// <summary>
        ///     A Guid that identifies the programming language used in the source document. Typically used by a debugger to locate
        ///     language specific logic.
        /// </summary>
        public Guid Language;

        /// <summary>
        ///     A Guid that identifies the compiler vendor programming language used in the source document. Typically used by a
        ///     debugger to locate vendor specific logic.
        /// </summary>
        public Guid LanguageVendor;

        /// <summary>
        ///     The line number corresponding to the first character in Text. Typically 1 but can be changed by C# preprocessor
        ///     directives.
        /// </summary>
        public int LineNumber;

        /// <summary>
        ///     An array of offsets, with offset at index i corresponding to the position of the first character of line i,
        ///     (counting lines from 0).
        /// </summary>
        private int[] lineOffsets;

        /// <summary>The number of lines in Text.</summary>
        private int lines;

        /// <summary>
        ///     The name of the document. Typically a file name. Can be a full or relative file path, or a URI or some other kind
        ///     of identifier.
        /// </summary>
        public string /*!*/
            Name;

        /// <summary>
        ///     Contains the source text.
        /// </summary>
        public DocumentText Text;

        private int uniqueKey;

        public Document()
        {
            Name = "";
            //^ base();
        }

        public Document(string /*!*/ name, int lineNumber, string text, Guid documentType, Guid language,
            Guid languageVendor)
            : this(name, lineNumber, new DocumentText(text), documentType, language, languageVendor)
        {
        }

        public Document(string /*!*/ name, int lineNumber, DocumentText text, Guid documentType, Guid language,
            Guid languageVendor)
        {
            DocumentType = documentType;
            Language = language;
            LanguageVendor = languageVendor;
            LineNumber = lineNumber;
            Name = name;
            Text = text;
            //^ base();
        }

        /// <summary>
        ///     An integer that uniquely distinguishes this document instance from every other document instance.
        ///     This provides an efficient equality test to facilitate hashing.
        /// </summary>
        public int UniqueKey
        {
            get
            {
                if (uniqueKey == 0)
                {
                    TryAgain:
                    var c = uniqueKeyCounter;
                    var cp1 = c == int.MaxValue ? 1 : c + 1;
                    if (Interlocked.CompareExchange(ref uniqueKeyCounter, cp1, c) != c) goto TryAgain;
                    uniqueKey = cp1;
                }

                return uniqueKey;
            }
        }

        /// <summary>
        ///     Maps the given zero based character position to the number of the source line containing the same character.
        ///     Line number counting starts from the value of LineNumber.
        /// </summary>
        public virtual int GetLine(int position)
        {
            var line = 0;
            var column = 0;
            GetPosition(position, out line, out column);
            return line + LineNumber;
        }

        /// <summary>
        ///     Maps the given zero based character position in the entire text to the position of the same character in a source
        ///     line.
        ///     Counting within the source line starts at 1.
        /// </summary>
        public virtual int GetColumn(int position)
        {
            var line = 0;
            var column = 0;
            GetPosition(position, out line, out column);
            return column + 1;
        }

        /// <summary>
        ///     Given a startLine, startColum, endLine and endColumn, this returns the corresponding startPos and endPos. In other
        ///     words it
        ///     converts a range expression in line and columns to a range expressed as a start and end character position.
        /// </summary>
        /// <param name="startLine">
        ///     The number of the line containing the first character. The number of the first line equals
        ///     this.LineNumber.
        /// </param>
        /// <param name="startColumn">The position of the first character relative to the start of the line. Counting from 1.</param>
        /// <param name="endLine">
        ///     The number of the line contain the character that immediate follows the last character of the
        ///     range.
        /// </param>
        /// <param name="endColumn">
        ///     The position, in the last line, of the character that immediately follows the last character of
        ///     the range.
        /// </param>
        /// <param name="startPos">The position in the entire text of the first character of the range, counting from 0.</param>
        /// <param name="endPos">The position in the entire text of the character following the last character of the range.</param>
        public virtual void GetOffsets(int startLine, int startColumn, int endLine, int endColumn, out int startPos,
            out int endPos)
        {
            lock (this)
            {
                if (lineOffsets == null) ComputeLineOffsets();
                //^ assert this.lineOffsets != null;
                startPos = lineOffsets[startLine - LineNumber] + startColumn - 1;
                endPos = lineOffsets[endLine - LineNumber] + endColumn - 1;
            }
        }

        /// <summary>
        ///     Retrieves a substring from the text of this Document. The substring starts at a specified character position and
        ///     has a specified length.
        /// </summary>
        public virtual string Substring(int position, int length)
        {
            if (Text == null) return null;
            return Text.Substring(position, length);
        }

        /// <summary>
        ///     Counts the number of end of line marker sequences in the given text.
        /// </summary>
        protected static int GetLineCount(string /*!*/ text)
        {
            var n = text == null ? 0 : text.Length;
            var count = 0;
            for (var i = 0; i < n; i++)
                switch (text[i])
                {
                    case '\r':
                        if (i + 1 < n && text[i + 1] == '\n')
                            i++;
                        count++;
                        break;
                    case '\n':
                    case (char)0x2028:
                    case (char)0x2029:
                        count++;
                        break;
                }

            return count;
        }

        /// <summary>
        ///     Returns the index in this.lineOffsets array such that this.lineOffsets[index] is less than or equal to offset
        ///     and offset is less than lineOffsets[index+1]
        /// </summary>
        private int Search(int offset)
        {
            Contract.Requires(offset >= 0);

            tryAgain:
            var lineOffsets = this.lineOffsets;
            var lines = this.lines;
            if (lineOffsets == null)
            {
                Debug.Assert(false);
                return -1;
            }

            if (offset < 0)
            {
                Debug.Assert(false);
                return -1;
            }

            var mid = 0;
            var low = 0;
            var high = lines - 1;
            while (low < high)
            {
                mid = (low + high) / 2;
                if (lineOffsets[mid] <= offset)
                {
                    if (offset < lineOffsets[mid + 1])
                        return mid;
                    low = mid + 1;
                }
                else
                {
                    high = mid;
                }
            }

            Debug.Assert(lines == this.lines);
            Debug.Assert(lineOffsets[low] <= offset);
            Debug.Assert(offset < lineOffsets[low + 1]);
            if (lineOffsets != this.lineOffsets) goto tryAgain;
            return low;
        }

        /// <summary>
        ///     Maps the given zero based character position in the entire text to a (line, column) pair corresponding to the same
        ///     position.
        ///     Counting within the source line starts at 0. Counting source lines start at 0.
        /// </summary>
        private void GetPosition(int offset, out int line, out int column)
        {
            line = 0;
            column = 0;
            if (offset < 0 || Text == null || offset > Text.Length)
            {
                Debug.Assert(false);
                return;
            }

            lock (this)
            {
                if (this.lineOffsets == null) ComputeLineOffsets();
                if (this.lineOffsets == null)
                {
                    Debug.Assert(false);
                    return;
                }

                var lineOffsets = this.lineOffsets;
                var index = Search(offset);
                Debug.Assert(lineOffsets == this.lineOffsets);
                if (index < 0 || index >= this.lineOffsets.Length)
                {
                    Debug.Assert(false);
                    return;
                }

                Debug.Assert(this.lineOffsets[index] <= offset && offset < this.lineOffsets[index + 1]);
                line = index;
                column = offset - this.lineOffsets[index];
            }
        }

        /// <summary>
        ///     Adds the given offset to the this.lineOffsets table as the offset corresponding to the start of line this.lines+1.
        /// </summary>
        private void AddOffset(int offset)
        {
            if (lineOffsets == null || lines < 0)
            {
                Debug.Assert(false);
                return;
            }

            if (lines >= lineOffsets.Length)
            {
                var n = lineOffsets.Length;
                if (n <= 0) n = 16;
                var newLineOffsets = new int[n * 2];
                Array.Copy(lineOffsets, newLineOffsets, lineOffsets.Length);
                lineOffsets = newLineOffsets;
            }

            lineOffsets[lines++] = offset;
        }

        public virtual void InsertOrDeleteLines(int offset, int lineCount)
        {
            if (lineCount == 0) return;
            if (offset < 0 || Text == null || offset > Text.Length)
            {
                Debug.Assert(false);
                return;
            }

            lock (this)
            {
                if (lineOffsets == null)
                    if (lineOffsets == null)
                        ComputeLineOffsets();
                if (lineCount < 0)
                    DeleteLines(offset, -lineCount);
                else
                    InsertLines(offset, lineCount);
            }
        }

        private void DeleteLines(int offset, int lineCount)
            //^ requires offset >= 0 && this.Text != null && offset < this.Text.Length && lineCount > 0 && this.lineOffsets != null;
        {
            Contract.Requires(offset >= 0);
            Contract.Requires(lineCount > 0);

            Contract.Assume(Text != null && offset < Text.Length && lineOffsets != null);

            //Debug.Assert(offset >= 0 && this.Text != null && offset < this.Text.Length && lineCount > 0 && this.lineOffsets != null);
            var index = Search(offset);
            if (index < 0 || index >= lines)
            {
                Debug.Assert(false);
                return;
            }

            for (var i = index + 1; i + lineCount < lines; i++) lineOffsets[i] = lineOffsets[i + lineCount];
            lines -= lineCount;
            if (lines <= index)
            {
                Debug.Assert(false);
                lines = index + 1;
            }
        }

        private void InsertLines(int offset, int lineCount)
            //^ requires offset >= 0 && this.Text != null && offset < this.Text.Length && lineCount > 0 && this.lineOffsets != null;
        {
            Debug.Assert(offset >= 0 && Text != null && offset < Text.Length && lineCount > 0 && lineOffsets != null);
            var index = Search(offset);
            if (index < 0 || index >= lines)
            {
                Debug.Assert(false);
                return;
            }

            var n = lineOffsets[lines - 1];
            for (var i = 0; i < lineCount; i++) AddOffset(++n);
            for (var i = lineCount; i > 0; i--) lineOffsets[index + i + 1] = lineOffsets[index + 1];
        }

        /// <summary>
        ///     Populates this.lineOffsets with an array of offsets, with offset at index i corresponding to the position of the
        ///     first
        ///     character of line i, (counting lines from 0).
        /// </summary>
        private void ComputeLineOffsets()
            //ensures this.lineOffsets != null;
        {
            if (Text == null)
            {
                Debug.Assert(false);
                return;
            }

            var n = Text.Length;
            lineOffsets = new int[n / 10 + 1];
            lines = 0;
            AddOffset(0);
            for (var i = 0; i < n; i++)
                switch (Text[i])
                {
                    case '\r':
                        if (i + 1 < n && Text[i + 1] == '\n')
                            i++;
                        AddOffset(i + 1);
                        break;
                    case '\n':
                    case (char)0x2028:
                    case (char)0x2029:
                        AddOffset(i + 1);
                        break;
                }

            AddOffset(n + 1);
            AddOffset(n + 2);
        }
    }

    /// <summary>
    ///     Can be used to mark magic hidden source contexts that are ignored by
    ///     debuggers and code coverage tools
    /// </summary>
    public class HiddenDocument : Document
    {
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly HiddenDocument Document = new HiddenDocument();

        private HiddenDocument()
        {
            Hidden = true;
        }

        public override int GetColumn(int position)
        {
            return 1;
        }

        public override int GetLine(int position)
        {
            return 0xFEEFEE;
        }
    }
#endif

#if !MinimalReader && !CodeContracts
  /// <summary>
  /// For creating source contexts that have just a filename, start line and column and end line and column.
  /// If a SourceContext has a DocumentWithPrecomputedLineNumbers as its Document, then it should have 0 as its StartPos
  /// and 1 as its EndPos because those are used here to decide what to return.
  /// </summary>
  public class DocumentWithPrecomputedLineNumbers : Document {
    private int startLine, startCol, endLine, endCol;
    public DocumentWithPrecomputedLineNumbers(string/*!*/ filename, int startLine, int startCol, int endLine, int endCol) {
      this.Name = filename;
      this.startLine = startLine;
      this.startCol = startCol;
      this.endLine = endLine;
      this.endCol = endCol;
    }
    public override int GetColumn (int offset) { return offset == 0 ? this.startCol : this.endCol; }
    public override int GetLine (int offset) { return offset == 0 ? this.startLine : this.endLine; }
  }
#endif
#if !ROTOR
    internal class UnmanagedDocument : Document
    {
        private readonly Int32List /*!*/
            columnList = new Int32List(8);

        private readonly Int32List /*!*/
            lineList = new Int32List(8);

        private UnmanagedDocument(IntPtr ptrToISymUnmanagedDocument)
        {
            //^ base();
            var idoc =
                (ISymUnmanagedDocument)Marshal.GetTypedObjectForIUnknown(ptrToISymUnmanagedDocument,
                    typeof(ISymUnmanagedDocument));
            if (idoc != null)
                try
                {
#if !FxCop
                    idoc.GetDocumentType(out DocumentType);
                    idoc.GetLanguage(out Language);
                    idoc.GetLanguageVendor(out LanguageVendor);
#endif
#if true
                    char[] buffer = null;
                    uint len = 0;
                    // get the size
                    idoc.GetURL(0, out len, buffer);
                    if (len > 0)
                    {
                        buffer = new char[len];
                        uint finalLen = 0;
                        idoc.GetURL(len, out finalLen, buffer);
                        Debug.Assert(len == finalLen);
                    }
#else
          uint capacity = 1024;
          uint len = 0;
          char[] buffer = new char[capacity];
          while (capacity >= 1024){
            idoc.GetURL(capacity, out len, buffer);
            if (len < capacity) break;
            capacity += 1024;
            buffer = new char[capacity];
          }
#endif
                    if (len > 0)
                        Name = new string(buffer, 0, (int)len - 1);
                }
                finally
                {
                    Marshal.ReleaseComObject(idoc);
                }
#if !FxCop && !CodeContracts
      this.LineNumber = -1;
      this.Text = null;
#endif
        }

        internal int GetOffset(uint line, uint column)
        {
            lineList.Add((int)line);
            columnList.Add((int)column);
            return lineList.Count - 1;
        }

        internal static UnmanagedDocument For(Dictionary<IntPtr, UnmanagedDocument> documentCache, IntPtr intPtr)
        {
            Contract.Requires(documentCache != null);

            UnmanagedDocument result;


            if (!documentCache.TryGetValue(intPtr, out result))
            {
                result = new UnmanagedDocument(intPtr);
                documentCache[intPtr] = result;
            }
            else
            {
#if DEBUG
                // double check
                var result2 = new UnmanagedDocument(intPtr);
                Debug.Assert(result.Name == result2.Name);
#endif
            }

            return result;
        }
#if !FxCop
        public override int GetLine(int offset)
        {
            return lineList[offset];
        }

        public override int GetColumn(int offset)
        {
            return columnList[offset];
        }

        public override void GetOffsets(int startLine, int startColumn, int endLine, int endColumn, out int startCol,
            out int endCol)
        {
            var i = BinarySearch(lineList, startLine);
            var columnList = this.columnList;
            startCol = 0;
            for (int j = i, n = columnList.Count; j < n; j++)
                if (columnList[j] >= startColumn)
                {
                    startCol = j;
                    break;
                }

            endCol = 0;
            i = BinarySearch(lineList, endLine);
            for (int j = i, n = columnList.Count; j < n; j++)
                if (columnList[j] >= endColumn)
                {
                    endCol = j;
                    break;
                }
        }

        private static int BinarySearch(Int32List /*!*/ list, int value)
        {
            Contract.Requires(list != null);

            var mid = 0;
            var low = 0;
            var high = list.Count - 1;
            while (low < high)
            {
                mid = low + (high - low) / 2;
                if (list[mid] <= value)
                {
                    if (list[mid + 1] > value)
                        return mid;
                    low = mid + 1;
                }
                else
                {
                    high = mid;
                }
            }

            return low;
        }

        public override void InsertOrDeleteLines(int offset, int lineCount)
        {
            Debug.Assert(false); //Caller should not be modifying an umanaged document
        }
#endif
    }
#endif // !ROTOR

#if FxCop
  public struct SourceContext{ 
    private string name;
    private int startLine;
    private int endLine;
    private int startColumn;
    private int endColumn;
    internal SourceContext(string name, uint startLine, uint endLine, uint startColumn, uint endColumn){  
      this.name = name;
      {
        this.startLine = (int)startLine;
        this.endLine = (int)endLine;
        this.startColumn = (int)startColumn;
        this.endColumn = (int)endColumn;
      }    
    }
    public string FileName{
      get{return this.name;}
    }
    public int StartLine{
      get{return this.startLine;}
    }
    public int EndLine{
      get{return this.endLine;}
    }
    public int StartColumn{
      get{return this.startColumn;}
    }
    public int EndColumn{
      get{return this.endColumn;}
    }
    public bool IsValid {
      get { return name != null && this.startLine != 0xFEEFEE; }
    }

  }
#else
    /// <summary>
    ///     Records a location within a source document that corresponds to an Abstract Syntax Tree node.
    /// </summary>
    public struct SourceContext
    {
        /// <summary>
        ///     The source document within which the AST node is located. Null if the node is not derived from a source
        ///     document.
        /// </summary>
        public Document Document;

        /// <summary>
        ///     The zero based index of the first character beyond  the last character in the source document that corresponds to
        ///     the AST node.
        /// </summary>
        public int EndPos;

        /// <summary>
        ///     The zero based index of the first character in the source document that corresponds to the AST node.
        /// </summary>
        public int StartPos;

        public bool Hidden => (StartPos < 0 && EndPos < 0) || Document == HiddenDocument.Document;

        public SourceContext(Document document)
            : this(document, 0, document == null ? 0 : document.Text == null ? 0 : document.Text.Length)
        {
        }

        public SourceContext(Document document, int startPos, int endPos)
        {
            Document = document;
            StartPos = startPos;
            EndPos = endPos;
        }

        public SourceContext(Document /*!*/ document,
            int startLine, int startColumn, int endLine, int endColumn)
        {
            Document = document;
            Document.GetOffsets(startLine, startColumn, endLine, endColumn, out StartPos, out EndPos);
        }

        /// <summary>
        ///     The number (counting from Document.LineNumber) of the line containing the first character in the source document
        ///     that corresponds to the AST node.
        /// </summary>
        public int StartLine
        {
            get
            {
                if (Hidden) return 0xFEEFEE;
                if (Document == null) return 0;
                return Document.GetLine(StartPos);
            }
        }

        /// <summary>
        ///     The number (counting from one) of the line column containing the first character in the source document that
        ///     corresponds to the AST node.
        /// </summary>
        public int StartColumn
        {
            get
            {
                if (!IsValid) return 0;
                return Document.GetColumn(StartPos);
            }
        }

        /// <summary>
        ///     The number (counting from Document.LineNumber) of the line containing the first character beyond the last character
        ///     in the source document that corresponds to the AST node.
        /// </summary>
        public int EndLine
        {
            get
            {
#if !CodeContracts
#if !ROTOR
        if (this.Document == null || (this.Document.Text == null && !(this.Document is UnmanagedDocument))) return 0;
#else
        if (this.Document == null || this.Document.Text == null) return 0;
#endif
        if (this.Document.Text != null && this.EndPos >= this.Document.Text.Length) this.EndPos =
 this.Document.Text.Length;
#endif
                if (Hidden) return 0xFEEFEE;
                if (Document == null) return 0;
                return Document.GetLine(EndPos);
            }
        }

        /// <summary>
        ///     The number (counting from one) of the line column containing first character beyond the last character in the
        ///     source document that corresponds to the AST node.
        /// </summary>
        public int EndColumn
        {
            get
            {
#if !CodeContracts
#if !ROTOR
        if (this.Document == null || (this.Document.Text == null && !(this.Document is UnmanagedDocument))) return 0;
#else
        if (this.Document == null || this.Document.Text == null) return 0;
#endif
        if (this.Document.Text != null && this.EndPos >= this.Document.Text.Length) this.EndPos =
 this.Document.Text.Length;
#endif
                if (!IsValid) return 0;
                return Document.GetColumn(EndPos);
            }
        }

        /// <summary>
        ///     Returns true if the line and column is greater than or equal the position of the first character
        ///     and less than or equal to the position of the last character
        ///     of the source document that corresponds to the AST node.
        /// </summary>
        /// <param name="line">A line number(counting from Document.LineNumber)</param>
        /// <param name="column">A column number (counting from one)</param>
        /// <returns></returns>
        public bool Encloses(int line, int column)
        {
            if (line < StartLine || line > EndLine) return false;
            if (line == StartLine) return column >= StartColumn && (column <= EndColumn || line < EndLine);
            if (line == EndLine) return column <= EndColumn;
            return true;
        }

        public bool Encloses(SourceContext sourceContext)
        {
            return StartPos <= sourceContext.StartPos && EndPos >= sourceContext.EndPos &&
                   EndPos > sourceContext.StartPos;
        }

        public bool IsValid => Document != null && StartLine != 0xFEEFEE;

        /// <summary>
        ///     The substring of the source document that corresponds to the AST node.
        /// </summary>
        public string SourceText
        {
            get
            {
                if (!IsValid) return null;
                return Document.Substring(StartPos, EndPos - StartPos);
            }
        }

        public void Hide()
        {
            StartPos = -1;
            EndPos = -1;
        }
    }
#endif
#if !MinimalReader
    public struct SourceChange
    {
        public SourceContext SourceContext;
        public string ChangedText;
    }

    /// <summary>
    ///     Allows a compilation to output progress messages and to query if cancellation was requested.
    /// </summary>
    public class CompilerSite
    {
        public virtual bool ShouldCancel => false;

        public virtual void OutputMessage(string message)
        {
        }
    }
#endif
#if !NoWriter
    public enum PlatformType
    {
        notSpecified,
        v1,
        v11,
        v2,
        cli1
    }

    public class CompilerOptions : CompilerParameters
    {
        public StringCollection AliasesForReferencedAssemblies;
        public ModuleKindFlags ModuleKind = ModuleKindFlags.ConsoleApplication;
        public bool EmitManifest = true;
        public StringList DefinedPreProcessorSymbols;
        public string XMLDocFileName;
        public string RecursiveWildcard;
        public StringList ReferencedModules;
        public string Win32Icon;
#if !WHIDBEY
    private StringCollection embeddedResources = new StringCollection();
    public StringCollection EmbeddedResources{
      get{return this.embeddedResources;}
    }
    private StringCollection linkedResources = new StringCollection();
    public StringCollection LinkedResources{
      get{return this.linkedResources;}
    }
#endif
#if VS7
    private System.Security.Policy.Evidence evidence;
    public System.Security.Policy.Evidence Evidence{
      get{return this.evidence;}
      set{this.evidence = value;}
    }
#endif
        public bool PDBOnly;
        public bool Optimize;
        public bool IncrementalCompile;
        public Int32List SuppressedWarnings;
        public bool CheckedArithmetic;
        public bool AllowUnsafeCode;
        public bool DisplayCommandLineHelp;
        public bool SuppressLogo;
        public long BaseAddress; //TODO: default value
        public string BugReportFileName;
        public object CodePage; //must be an int if not null
        public bool EncodeOutputInUTF8;
        public bool FullyQualifyPaths;
        public int FileAlignment;
        public bool NoStandardLibrary;
        public StringList AdditionalSearchPaths;
        public bool HeuristicReferenceResolution;
        public string RootNamespace;
        public bool CompileAndExecute;
        public object UserLocaleId; //must be an int if not null
        public string StandardLibraryLocation;
        public PlatformType TargetPlatform; //TODO: rename this to TargetRuntime
#if !MinimalReader
        public ProcessorType TargetProcessor;
#endif
        public string TargetPlatformLocation;
        public string AssemblyKeyFile;
        public string AssemblyKeyName;
        public bool DelaySign;
        public TargetInformation TargetInformation;
        public Int32List SpecificWarningsToTreatAsErrors;
        public Int32List SpecificWarningsNotToTreatAsErrors;
        public string OutputPath;
        public string ExplicitOutputExtension;
        public AppDomain TargetAppDomain;
        public bool MayLockFiles;
        public string ShadowedAssembly;
        public bool UseStandardConfigFile;
#if !MinimalReader
        public CompilerSite Site;
#endif
#if ExtendedRuntime
    /// <summary>
    /// True if the source code for the assembly specify only contracts.
    /// </summary>
    public bool IsContractAssembly;
    /// <summary>
    /// Do not emit run-time checks for requires clauses of non-externally-accessible methods, assert statements, loop invariants, and ensures clauses.
    /// </summary>
    public bool DisableInternalChecks;
    /// <summary>
    /// Do not emit run-time checks for assume statements.
    /// </summary>
    public bool DisableAssumeChecks;
    /// <summary>
    /// Do not emit run-time checks for requires clauses of externally accessible methods.
    /// Do not emit run-time checks that enforce checked exception policy.
    /// </summary>
    public bool DisableDefensiveChecks;
    /// <summary>
    /// Disable the guarded classes feature, which integrates run-time enforcement of object invariants, ownership, and safe concurrency.
    /// </summary>
    public bool DisableGuardedClassesChecks;
    public bool DisableInternalContractsMetadata;
    public bool DisablePublicContractsMetadata;
    /// <summary>
    /// Disable the runtime test against null on non-null typed parameters on public methods
    /// </summary>
    public bool DisableNullParameterValidation;
    public virtual bool LoadDebugSymbolsForReferencedAssemblies {
      get { return false; } 
    }

    /// <summary>
    /// If set, the compiler will only parse and then emit an xml file with detailed source contexts 
    /// about what is parsed.
    /// </summary>
    public bool EmitSourceContextsOnly = false;

#endif
        public CompilerOptions()
        {
        }

        public CompilerOptions(CompilerOptions source)
        {
            if (source == null)
            {
                Debug.Assert(false);
                return;
            }

            AdditionalSearchPaths = source.AdditionalSearchPaths; //REVIEW: clone the list?
            AliasesForReferencedAssemblies = source.AliasesForReferencedAssemblies;
            AllowUnsafeCode = source.AllowUnsafeCode;
            AssemblyKeyFile = source.AssemblyKeyFile;
            AssemblyKeyName = source.AssemblyKeyName;
            BaseAddress = source.BaseAddress;
            BugReportFileName = source.BugReportFileName;
            CheckedArithmetic = source.CheckedArithmetic;
            CodePage = source.CodePage;
            CompileAndExecute = source.CompileAndExecute;
            CompilerOptions = source.CompilerOptions;
            DefinedPreProcessorSymbols = source.DefinedPreProcessorSymbols;
            DelaySign = source.DelaySign;
#if ExtendedRuntime
      this.DisableAssumeChecks = source.DisableAssumeChecks;
      this.DisableDefensiveChecks = source.DisableDefensiveChecks;
      this.DisableGuardedClassesChecks = source.DisableGuardedClassesChecks;
      this.DisableInternalChecks = source.DisableInternalChecks;
      this.DisableInternalContractsMetadata = source.DisableInternalContractsMetadata;
      this.DisablePublicContractsMetadata = source.DisablePublicContractsMetadata;
#endif
            DisplayCommandLineHelp = source.DisplayCommandLineHelp;
            if (source.EmbeddedResources != null)
                foreach (var s in source.EmbeddedResources)
                    EmbeddedResources.Add(s);
            EmitManifest = source.EmitManifest;
            EncodeOutputInUTF8 = source.EncodeOutputInUTF8;
            ExplicitOutputExtension = source.ExplicitOutputExtension;
            FileAlignment = source.FileAlignment;
            FullyQualifyPaths = source.FullyQualifyPaths;
            GenerateExecutable = source.GenerateExecutable;
            GenerateInMemory = source.GenerateInMemory;
            HeuristicReferenceResolution = source.HeuristicReferenceResolution;
            IncludeDebugInformation = source.IncludeDebugInformation;
            IncrementalCompile = source.IncrementalCompile;
#if ExtendedRuntime
      this.IsContractAssembly = source.IsContractAssembly;
#endif
            if (source.LinkedResources != null)
                foreach (var s in source.LinkedResources)
                    LinkedResources.Add(s);
            MainClass = source.MainClass;
            MayLockFiles = source.MayLockFiles;
            ModuleKind = source.ModuleKind;
            NoStandardLibrary = source.NoStandardLibrary;
            Optimize = source.Optimize;
            OutputAssembly = source.OutputAssembly;
            OutputPath = source.OutputPath;
            PDBOnly = source.PDBOnly;
            RecursiveWildcard = source.RecursiveWildcard;
            if (source.ReferencedAssemblies != null)
                foreach (var s in source.ReferencedAssemblies)
                    ReferencedAssemblies.Add(s);
            ReferencedModules = source.ReferencedModules;
            RootNamespace = source.RootNamespace;
            ShadowedAssembly = source.ShadowedAssembly;
            SpecificWarningsToTreatAsErrors = source.SpecificWarningsToTreatAsErrors;
            StandardLibraryLocation = source.StandardLibraryLocation;
            SuppressLogo = source.SuppressLogo;
            SuppressedWarnings = source.SuppressedWarnings;
            TargetAppDomain = source.TargetAppDomain;
            TargetInformation = source.TargetInformation;
            TargetPlatform = source.TargetPlatform;
            TargetPlatformLocation = source.TargetPlatformLocation;
            TreatWarningsAsErrors = source.TreatWarningsAsErrors;
            UserLocaleId = source.UserLocaleId;
            UserToken = source.UserToken;
            WarningLevel = source.WarningLevel;
            Win32Icon = source.Win32Icon;
            Win32Resource = source.Win32Resource;
            XMLDocFileName = source.XMLDocFileName;
        }

        public virtual string GetOptionHelp()
        {
            return null;
        }

        public virtual CompilerOptions Clone()
        {
            return (CompilerOptions)MemberwiseClone();
        }
    }
#endif
    public sealed class MarshallingInformation
    {
        public string Class { get; set; }

        public string Cookie { get; set; }

        public int ElementSize { get; set; }

        public NativeType ElementType { get; set; }

        public NativeType NativeType { get; set; }

        public int NumberOfElements { get; set; }

        public int ParamIndex { get; set; }

        public int Size { get; set; }

        public MarshallingInformation Clone()
        {
            return (MarshallingInformation)MemberwiseClone();
        }
    }
#if !NoWriter
    public struct TargetInformation
    {
        public string Company;
        public string Configuration;
        public string Copyright;
        public string Culture;
        public string Description;
        public string Product;
        public string ProductVersion;
        public string Title;
        public string Trademark;
        public string Version;
    }
#endif
    public enum NativeType
    {
        Bool = 0x2, // 4 byte boolean value (true != 0, false == 0)
        I1 = 0x3, // 1 byte signed value
        U1 = 0x4, // 1 byte unsigned value
        I2 = 0x5, // 2 byte signed value
        U2 = 0x6, // 2 byte unsigned value
        I4 = 0x7, // 4 byte signed value
        U4 = 0x8, // 4 byte unsigned value
        I8 = 0x9, // 8 byte signed value
        U8 = 0xa, // 8 byte unsigned value
        R4 = 0xb, // 4 byte floating point
        R8 = 0xc, // 8 byte floating point
        Currency = 0xf, // A currency
        BStr = 0x13, // OLE Unicode BSTR
        LPStr = 0x14, // Ptr to SBCS string
        LPWStr = 0x15, // Ptr to Unicode string
        LPTStr = 0x16, // Ptr to OS preferred (SBCS/Unicode) string
        ByValTStr = 0x17, // OS preferred (SBCS/Unicode) inline string (only valid in structs)
        IUnknown = 0x19, // COM IUnknown pointer. 
        IDispatch = 0x1a, // COM IDispatch pointer
        Struct = 0x1b, // Structure
        Interface = 0x1c, // COM interface
        SafeArray = 0x1d, // OLE SafeArray
        ByValArray = 0x1e, // Array of fixed size (only valid in structs)
        SysInt = 0x1f, // Hardware natural sized signed integer
        SysUInt = 0x20,
        VBByRefStr = 0x22,
        AnsiBStr = 0x23, // OLE BSTR containing SBCS characters
        TBStr = 0x24, // Ptr to OS preferred (SBCS/Unicode) BSTR
        VariantBool = 0x25, // OLE defined BOOLEAN (2 bytes, true == -1, false == 0)
        FunctionPtr = 0x26, // Function pointer
        AsAny = 0x28, // Paired with Object type and does runtime marshalling determination
        LPArray = 0x2a, // C style array
        LPStruct = 0x2b, // Pointer to a structure
        CustomMarshaler = 0x2c, // Native type supplied by custom code   
        Error = 0x2d,
        NotSpecified = 0x50
    }

    ///0-: Common
    ///1000-: HScript
    ///2000-: EcmaScript
    ///3000-: Zonnon
    ///4000-: Comega
    ///5000-: X++
    ///6000-: Spec#
    ///7000-: Sing#
    ///8000-: Xaml
    ///9000-: C/AL
    ///For your range contact hermanv@microsoft.com
    public enum NodeType
    {
        //Dummy
        Undefined = 0,

        //IL instruction node tags
        Add,
        Add_Ovf,
        Add_Ovf_Un,
        And,
        Arglist,
        Box,
        Branch,
        Call,
        Calli,
        Callvirt,
        Castclass,
        Ceq,
        Cgt,
        Cgt_Un,
        Ckfinite,
        Clt,
        Clt_Un,
        Conv_I,
        Conv_I1,
        Conv_I2,
        Conv_I4,
        Conv_I8,
        Conv_Ovf_I,
        Conv_Ovf_I_Un,
        Conv_Ovf_I1,
        Conv_Ovf_I1_Un,
        Conv_Ovf_I2,
        Conv_Ovf_I2_Un,
        Conv_Ovf_I4,
        Conv_Ovf_I4_Un,
        Conv_Ovf_I8,
        Conv_Ovf_I8_Un,
        Conv_Ovf_U,
        Conv_Ovf_U_Un,
        Conv_Ovf_U1,
        Conv_Ovf_U1_Un,
        Conv_Ovf_U2,
        Conv_Ovf_U2_Un,
        Conv_Ovf_U4,
        Conv_Ovf_U4_Un,
        Conv_Ovf_U8,
        Conv_Ovf_U8_Un,
        Conv_R_Un,
        Conv_R4,
        Conv_R8,
        Conv_U,
        Conv_U1,
        Conv_U2,
        Conv_U4,
        Conv_U8,
        Cpblk,
        DebugBreak,
        Div,
        Div_Un,
        Dup,
        EndFilter,
        EndFinally,
        ExceptionHandler,
        Initblk,
        Isinst,
        Jmp,
        Ldftn,
        Ldlen,
        Ldtoken,
        Ldvirtftn,
        Localloc,
        Mkrefany,
        Mul,
        Mul_Ovf,
        Mul_Ovf_Un,
        Neg,
        Nop,
        Not,
        Or,
        Pop,
        ReadOnlyAddressOf,
        Refanytype,
        Refanyval,
        Rem,
        Rem_Un,
        Rethrow,
        Shl,
        Shr,
        Shr_Un,
        Sizeof,
        SkipCheck,
        Sub,
        Sub_Ovf,
        Sub_Ovf_Un,
        SwitchInstruction,
        Throw,
        Unbox,
        UnboxAny,
        Xor,

        //AST tags that are relevant to the binary reader
        AddressDereference,
        AddressOf,
        AssignmentStatement,
        Block,
        Catch,
        Construct,
        ConstructArray,
        Eq,
        ExpressionStatement,
        FaultHandler,
        Filter,
        Finally,
        Ge,
        Gt,
        Identifier,
        Indexer,
        Instruction,
        InterfaceExpression,
        Le,
        Literal,
        LogicalNot,
        Lt,
        MemberBinding,
        NamedArgument,
        Namespace,
        Ne,
        Return,
        This,
        Try,

        //Metadata node tags
        ArrayType,
        Assembly,
        AssemblyReference,
        Attribute,
        Class,
        ClassParameter,
        DelegateNode,
        EnumNode,
        Event,
        Field,
        FunctionPointer,
        InstanceInitializer,
        Interface,
        Local,
        Method,
        Module,
        ModuleReference,
        OptionalModifier,
        Parameter,
        Pointer,
        Property,
        Reference,
        RequiredModifier,
        SecurityAttribute,
        StaticInitializer,
        Struct,
        TypeParameter,

#if !MinimalReader
        // The following NodeType definitions are not required
        // for examining assembly metadata directly from binaries

        //Serialization tags used for values that are not leaf nodes.
        Array,
        BlockReference,
        CompilationParameters,
        Document,
        EndOfRecord,
        Expression,
        Guid,
        List,
        MarshallingInformation,
        Member,
        MemberReference,
        MissingBlockReference,
        MissingExpression,
        MissingMemberReference,
        String,
        StringDictionary,
        TypeNode,
        Uri,
        XmlNode,

        //Source-based AST node tags
        AddEventHandler,
        AliasDefinition,
        AnonymousNestedFunction,
        ApplyToAll,
        ArglistArgumentExpression,
        ArglistExpression,
        ArrayTypeExpression,
        As,
        Assertion,
        AssignmentExpression,
        Assumption,
        Base,
#endif
#if FxCop
    BlockExpression,
    StackVariable,
#endif
#if !MinimalReader
        BlockExpression,
        BoxedTypeExpression,
        ClassExpression,
        CoerceTuple,
        CollectionEnumerator,
        Comma,
        Compilation,
        CompilationUnit,
        CompilationUnitSnippet,
        Conditional,
        ConstructDelegate,
        ConstructFlexArray,
        ConstructIterator,
        ConstructTuple,
        Continue,
        CopyReference,
        CurrentClosure,
        Decrement,
        DefaultValue,
        DoWhile,
        Exit,
        ExplicitCoercion,
        ExpressionSnippet,
        FieldInitializerBlock,
        Fixed,
        FlexArrayTypeExpression,
        For,
        ForEach,
        FunctionDeclaration,
        FunctionTypeExpression,
        Goto,
        GotoCase,
        If,
        ImplicitThis,
        Increment,
        InvariantTypeExpression,
        Is,
        LabeledStatement,
        LocalDeclaration,
        LocalDeclarationsStatement,
        Lock,
        LogicalAnd,
        LogicalOr,
        LRExpression,
        MethodCall,
        NameBinding,
        NonEmptyStreamTypeExpression,
        NonNullableTypeExpression,
        NonNullTypeExpression,
        NullableTypeExpression,
        NullCoalesingExpression,
        OutAddress,
        Parentheses,
        PointerTypeExpression,
        PostfixExpression,
        PrefixExpression,
        QualifiedIdentifer,
        RefAddress,
        ReferenceTypeExpression,
        RefTypeExpression,
        RefValueExpression,
        RemoveEventHandler,
        Repeat,
        ResourceUse,
        SetterValue,
        StackAlloc,
        StatementSnippet,
        StreamTypeExpression,
        Switch,
        SwitchCase,
        SwitchCaseBottom,
        TemplateInstance,
        TupleTypeExpression,
        TypeExpression,
        TypeIntersectionExpression,
        TypeMemberSnippet,
        Typeof,
        TypeReference,
        Typeswitch,
        TypeswitchCase,
        TypeUnionExpression,
        UnaryPlus,
        UsedNamespace,
        VariableDeclaration,
        While,
        Yield,

        //Extended metadata node tags
        ConstrainedType,
        TupleType,
        TypeAlias,
        TypeIntersection,
        TypeUnion,

        //Query node tags
        Composition,
        QueryAggregate,
        QueryAlias,
        QueryAll,
        QueryAny,
        QueryAxis,
        QueryCommit,
        QueryContext,
        QueryDelete,
        QueryDifference,
        QueryDistinct,
        QueryExists,
        QueryFilter,
        QueryGeneratedType,
        QueryGroupBy,
        QueryInsert,
        QueryIntersection,
        QueryIterator,
        QueryJoin,
        QueryLimit,
        QueryOrderBy,
        QueryOrderItem,
        QueryPosition,
        QueryProject,
        QueryQuantifiedExpression,
        QueryRollback,
        QuerySelect,
        QuerySingleton,
        QueryTransact,
        QueryTypeFilter,
        QueryUnion,
        QueryUpdate,
        QueryYielder,

        //Contract node tags
        Acquire,
        Comprehension,
        ComprehensionBinding,
        Ensures,
        EnsuresExceptional,
        EnsuresNormal,
        Iff,
        Implies,
        Invariant,
        LogicalEqual,
        LogicalImply,
        Maplet,
        MethodContract,
        Modelfield,
        ModelfieldContract,
        OldExpression,
        Range,
        Read,
        Requires,
        RequiresOtherwise,
        RequiresPlain,
        RequiresValidation,
        ReturnValue,
        TypeContract,
        Write,

        //Node tags for explicit modifiers in front-end
        OptionalModifierTypeExpression,
        RequiredModifierTypeExpression,

        //Temporary node tags
        Count,
        Exists,
        ExistsUnique,
        Forall,
        Max,
        Min,
        Product,
        Sum,
        Quantifier,
#endif // MinimalReader
    }

    [Flags]
    public enum AssemblyFlags
    {
        None = 0x0000,
        PublicKey = 0x0001,
        Library = 0x0002,
        Platform = 0x0004,
        NowPlatform = 0x0006,
        SideBySideCompatible = 0x0000,
        NonSideBySideCompatible = 0x0010,
        NonSideBySideProcess = 0x0020,
        NonSideBySideMachine = 0x0030,
        CompatibilityMask = 0x00F0,
        Retargetable = 0x0100,
        ContainsForeignTypes = 0x0200,
        DisableJITcompileOptimizer = 0x4000,
        EnableJITcompileTracking = 0x8000
    }

    public enum AssemblyHashAlgorithm
    {
        None = 0x0000,
        MD5 = 0x8003,
        SHA1 = 0x8004
    }

    [Flags]
    public enum CallingConventionFlags
    {
        Default = 0x0,
        C = 0x1,
        StandardCall = 0x2,
        ThisCall = 0x3,
        FastCall = 0x4,
        VarArg = 0x5,
        ArgumentConvention = 0x7,
        Generic = 0x10,
        HasThis = 0x20,
        ExplicitThis = 0x40
    }

    [Flags]
    public enum EventFlags
    {
        None = 0x0000,
        SpecialName = 0x0200,
        ReservedMask = 0x0400,
        RTSpecialName = 0x0400,
#if !MinimalReader
        Extend = MethodFlags.Extend, // used for languages with type extensions, e.g. Sing#
#endif
    }

    [Flags]
    public enum FieldFlags
    {
        None = 0x0000,
        FieldAccessMask = 0x0007,
        CompilerControlled = 0x0000,
        Private = 0x0001,
        FamANDAssem = 0x0002,
        Assembly = 0x0003,
        Family = 0x0004,
        FamORAssem = 0x0005,
        Public = 0x0006,
        Static = 0x0010,
        InitOnly = 0x0020,
        Literal = 0x0040,
        NotSerialized = 0x0080,
        SpecialName = 0x0200,
        PinvokeImpl = 0x2000,
        ReservedMask = 0x9500,
        RTSpecialName = 0x0400,
        HasFieldMarshal = 0x1000,
        HasDefault = 0x8000,
        HasFieldRVA = 0x0100
    }

    [Flags]
    public enum FileFlags
    {
        ContainsMetaData = 0x0000,
        ContainsNoMetaData = 0x0001
    }

    [Flags]
    public enum TypeParameterFlags
    {
        NonVariant = 0x0000,
        Covariant = 0x0001,
        Contravariant = 0x0002,
        VarianceMask = 0x0003,
        NoSpecialConstraint = 0x0000,
        ReferenceTypeConstraint = 0x0004,
        ValueTypeConstraint = 0x0008,
        DefaultConstructorConstraint = 0x0010,
        SpecialConstraintMask = 0x001C
    }

    [Flags]
    public enum MethodImplFlags
    {
        CodeTypeMask = 0x0003,
        IL = 0x0000,
        Native = 0x0001,
        OPTIL = 0x0002,
        Runtime = 0x0003,
        ManagedMask = 0x0004,
        Unmanaged = 0x0004,
        Managed = 0x0000,
        ForwardRef = 0x0010,
        PreserveSig = 0x0080,
        InternalCall = 0x1000,
        Synchronized = 0x0020,
        NoInlining = 0x0008,
#if !MinimalReader
        MaxMethodImplVal = 0xffff
#endif
    }

    [Flags]
    public enum MethodFlags
    {
        MethodAccessMask = 0x0007,
        CompilerControlled = 0x0000,
        Private = 0x0001,
        FamANDAssem = 0x0002,
        Assembly = 0x0003,
        Family = 0x0004,
        FamORAssem = 0x0005,
        Public = 0x0006,
        Static = 0x0010,
        Final = 0x0020,
        Virtual = 0x0040,
        HideBySig = 0x0080,
        VtableLayoutMask = 0x0100,
        ReuseSlot = 0x0000,
        NewSlot = 0x0100,
        CheckAccessOnOverride = 0x0200,
        Abstract = 0x0400,
        SpecialName = 0x0800,
        PInvokeImpl = 0x2000,
        UnmanagedExport = 0xd000,
        ReservedMask = 0xd000,
        RTSpecialName = 0x1000,
        HasSecurity = 0x4000,
        RequireSecObject = 0x8000,
#if !MinimalReader
        Extend = 0x01000000, // used for languages with type extensions, e.g. Sing#
#endif
    }

    public enum ModuleKindFlags
    {
        //TODO: rename this to just ModuleKind
        ConsoleApplication,
        WindowsApplication,
        DynamicallyLinkedLibrary,
        ManifestResourceFile,
        UnmanagedDynamicallyLinkedLibrary
    }

    [Flags]
    public enum ParameterFlags
    {
        None = 0x0000,
        In = 0x0001,
        Out = 0x0002,
        Optional = 0x0010,
        ReservedMask = 0xf000,
        HasDefault = 0x1000,
        HasFieldMarshal = 0x2000,

        ParameterNameMissing =
            0x4000 // for parameters that do not have a name in the metadata, even though internally we give them a name
    }

    [Flags]
    public enum PEKindFlags
    {
        ILonly = 0x0001,
        Requires32bits = 0x0002,
        Requires64bits = 0x0004,
        AMD = 0x0008,
        Prefers32bits = 0x00020000
    }

    [Flags]
    public enum PInvokeFlags
    {
        None = 0x0000,
        NoMangle = 0x0001,
        BestFitDisabled = 0x0020,
        BestFitEnabled = 0x0010,
        BestFitUseAsm = 0x0000,
        BestFitMask = 0x0030,
        CharSetMask = 0x0006,
        CharSetNotSpec = 0x0000,
        CharSetAns = 0x0002,
        CharSetUnicode = 0x0004,
        CharSetAuto = 0x0006,
        SupportsLastError = 0x0040,
        CallingConvMask = 0x0700,
        CallConvWinapi = 0x0100,
        CallConvCdecl = 0x0200,
        CallConvStdcall = 0x0300,
        CallConvThiscall = 0x0400,
        CallConvFastcall = 0x0500,
        ThrowOnUnmappableCharMask = 0x3000,
        ThrowOnUnmappableCharEnabled = 0x1000,
        ThrowOnUnmappableCharDisabled = 0x2000,
        ThrowOnUnmappableCharUseAsm = 0x0000
    }

    [Flags]
    public enum PropertyFlags
    {
        None = 0x0000,
        SpecialName = 0x0200,
        ReservedMask = 0xf400,
        RTSpecialName = 0x0400,
#if !MinimalReader
        Extend = MethodFlags.Extend, // used for languages with type extensions, e.g. Sing#
#endif
    }

    public enum PESection
    {
        Text,
        SData,
        TLS
    }
#if !MinimalReader
    public enum ProcessorType
    {
        Any,
        x86,
        x64,
        Itanium
    }
#endif
    [Flags]
    public enum TypeFlags
    {
        None = 0x00000000,
        VisibilityMask = 0x00000007,
        NotPublic = 0x00000000,
        Public = 0x00000001,
        NestedPublic = 0x00000002,
        NestedPrivate = 0x00000003,
        NestedFamily = 0x00000004,
        NestedAssembly = 0x00000005,
        NestedFamANDAssem = 0x00000006,
        NestedFamORAssem = 0x00000007,
        LayoutMask = 0x00000018,
        AutoLayout = 0x00000000,
        SequentialLayout = 0x00000008,
        ExplicitLayout = 0x00000010,
        ClassSemanticsMask = 0x00000020,
        Class = 0x00000000,
        Interface = 0x00000020,
        LayoutOverridden = 0x00000040, // even AutoLayout can be explicit or implicit
        Abstract = 0x00000080,
        Sealed = 0x00000100,
        SpecialName = 0x00000400,
        Import = 0x00001000,
        Serializable = 0x00002000,
        IsForeign = 0x00004000,
        StringFormatMask = 0x00030000,
        AnsiClass = 0x00000000,
        UnicodeClass = 0x00010000,
        AutoClass = 0x00020000,
        BeforeFieldInit = 0x00100000,
        ReservedMask = 0x00040800,
        RTSpecialName = 0x00000800,
        HasSecurity = 0x00040000,

        Forwarder =
            0x00200000, //The type is a stub left behind for backwards compatibility. References to this type are forwarded to another type by the CLR.
#if !MinimalReader
        Extend = 0x01000000, // used for languages with type extensions, e.g. Sing#
#endif
    }

    public sealed class TrivialHashtable
    {
        private const int InitialSize = 4;

        private HashEntry[] /*!*/
            entries;

        public TrivialHashtable()
        {
            entries = new HashEntry[InitialSize];
            //this.count = 0;
        }

        private TrivialHashtable(HashEntry[] /*!*/ entries, int count)
        {
            this.entries = entries;
            Count = count;
        }

        public TrivialHashtable(int expectedEntries)
        {
            var initialSize = 16;
            expectedEntries <<= 1;
            while (initialSize < expectedEntries && initialSize > 0) initialSize <<= 1;
            if (initialSize < 0) initialSize = InitialSize;
            entries = new HashEntry[initialSize];
            //this.count = 0;
        }

        public int Count { get; private set; }

        public object this[int key]
        {
            get
            {
                if (key <= 0) throw new ArgumentException(ExceptionStrings.KeyNeedsToBeGreaterThanZero, "key");
                var entries = this.entries;
                var n = entries.Length;
                var i = key & (n - 1);
                var k = entries[i].Key;
                object result = null;
                while (true)
                {
                    if (k == key)
                    {
                        result = entries[i].Value;
                        break;
                    }

                    if (k == 0) break;
                    i++;
                    if (i >= n) i = 0;
                    k = entries[i].Key;
                }

                return result;
            }
            set
            {
                if (key <= 0) throw new ArgumentException(ExceptionStrings.KeyNeedsToBeGreaterThanZero, "key");
                var entries = this.entries;
                var n = entries.Length;
                var i = key & (n - 1);
                var k = entries[i].Key;
                while (true)
                {
                    if (k == key || k == 0)
                    {
                        entries[i].Value = value;
                        if (k == 0)
                        {
                            if (value == null) return;
                            entries[i].Key = key;
                            if (++Count > n / 2) Expand();
                            return;
                        }

                        if (value == null) entries[i].Key = -1;
                        return;
                    }

                    i++;
                    if (i >= n) i = 0;
                    k = entries[i].Key;
                }
            }
        }

        public IEnumerable Values
        {
            get
            {
                for (var i = 0; i < entries.Length; i++)
                    if (entries[i].Key != 0)
                        yield return entries[i].Value;
            }
        }

        private void Expand()
        {
            var oldEntries = this.entries;
            var n = oldEntries.Length;
            var m = n * 2;
            if (m <= 0) return;
            var entries = new HashEntry[m];
            var count = 0;
            for (var i = 0; i < n; i++)
            {
                var key = oldEntries[i].Key;
                if (key <= 0) continue; //No entry (0) or deleted entry (-1)
                var value = oldEntries[i].Value;
                Debug.Assert(value != null);
                var j = key & (m - 1);
                var k = entries[j].Key;
                while (true)
                {
                    if (k == 0)
                    {
                        entries[j].Value = value;
                        entries[j].Key = key;
                        count++;
                        break;
                    }

                    j++;
                    if (j >= m) j = 0;
                    k = entries[j].Key;
                }
            }

            this.entries = entries;
            Count = count;
        }

        public TrivialHashtable Clone()
        {
            var clonedEntries = (HashEntry[])entries.Clone();
            //^ assume clonedEntries != null;
            return new TrivialHashtable(clonedEntries, Count);
        }

        private struct HashEntry
        {
            public int Key;
            public object Value;
        }
    }


    public sealed class TrivialHashtable<T> where T : struct
    {
        private const int InitialSize = 4;

        private HashEntry[] /*!*/
            entries;

        public TrivialHashtable()
        {
            entries = new HashEntry[InitialSize];
            //this.count = 0;
        }

        private TrivialHashtable(HashEntry[] /*!*/ entries, int count)
        {
            this.entries = entries;
            Count = count;
        }

        public TrivialHashtable(int expectedEntries)
        {
            var initialSize = 16;
            expectedEntries <<= 1;
            while (initialSize < expectedEntries && initialSize > 0) initialSize <<= 1;
            if (initialSize < 0) initialSize = InitialSize;
            entries = new HashEntry[initialSize];
            //this.count = 0;
        }

        public int Count { get; private set; }

        public T this[int key]
        {
            get
            {
                T result;
                TryGetValue(key, out result);
                return result;
            }
            set
            {
                if (key <= 0) throw new ArgumentException(ExceptionStrings.KeyNeedsToBeGreaterThanZero, "key");
                var entries = this.entries;
                var n = entries.Length;
                var i = key & (n - 1);
                var k = entries[i].Key;
                while (true)
                {
                    if (k == key || k == 0)
                    {
                        entries[i].Value = value;
                        if (k == 0)
                        {
                            entries[i].Key = key;
                            if (++Count > n / 2) Expand();
                            return;
                        }

                        return;
                    }

                    i++;
                    if (i >= n) i = 0;
                    k = entries[i].Key;
                }
            }
        }

        private void Expand()
        {
            var oldEntries = this.entries;
            var n = oldEntries.Length;
            var m = n * 2;
            if (m <= 0) return;
            var entries = new HashEntry[m];
            var count = 0;
            for (var i = 0; i < n; i++)
            {
                var key = oldEntries[i].Key;
                if (key <= 0) continue; //No entry (0) or deleted entry (-1)
                var value = oldEntries[i].Value;
                //Debug.Assert(value != null);
                var j = key & (m - 1);
                var k = entries[j].Key;
                while (true)
                {
                    if (k == 0)
                    {
                        entries[j].Value = value;
                        entries[j].Key = key;
                        count++;
                        break;
                    }

                    j++;
                    if (j >= m) j = 0;
                    k = entries[j].Key;
                }
            }

            this.entries = entries;
            Count = count;
        }

        public bool TryGetValue(int key, out T result)
        {
            if (key <= 0) throw new ArgumentException(ExceptionStrings.KeyNeedsToBeGreaterThanZero, "key");
            var entries = this.entries;
            var n = entries.Length;
            var i = key & (n - 1);
            var k = entries[i].Key;

            while (true)
            {
                if (k == key)
                {
                    result = entries[i].Value;
                    return true;
                }

                if (k == 0) break;
                i++;
                if (i >= n) i = 0;
                k = entries[i].Key;
            }

            result = default(T);
            return false;
        }

        public TrivialHashtable<T> Clone()
        {
            var clonedEntries = (HashEntry[])entries.Clone();
            //^ assume clonedEntries != null;
            return new TrivialHashtable<T>(clonedEntries, Count);
        }

        private struct HashEntry
        {
            public int Key;
            public T Value;
        }
    }


#if !FxCop
    public
#endif
        sealed class TrivialHashtableUsingWeakReferences
    {
        private HashEntry[] /*!*/
            entries;

        public TrivialHashtableUsingWeakReferences()
        {
            entries = new HashEntry[16];
            //this.count = 0;
        }

        private TrivialHashtableUsingWeakReferences(HashEntry[] /*!*/ entries, int count)
        {
            this.entries = entries;
            Count = count;
        }

        public TrivialHashtableUsingWeakReferences(int expectedEntries)
        {
            var initialSize = 16;
            expectedEntries <<= 1;
            while (initialSize < expectedEntries && initialSize > 0) initialSize <<= 1;
            if (initialSize < 0) initialSize = 16;
            entries = new HashEntry[initialSize];
            //this.count = 0;
        }

        public int Count { get; private set; }

        public object this[int key]
        {
            get
            {
                if (key <= 0) throw new ArgumentException(ExceptionStrings.KeyNeedsToBeGreaterThanZero, "key");
                var entries = this.entries;
                var n = entries.Length;
                var i = key & (n - 1);
                var k = entries[i].Key;
                object result = null;
                while (true)
                {
                    if (k == key)
                    {
                        var wref = entries[i].Value;
                        if (wref == null)
                        {
                            Debug.Assert(false);
                            return null;
                        }

                        result = wref.Target;
                        if (result != null) return result;
                        WeedOutCollectedEntries();
                        while (Count < n / 4 && n > 16)
                        {
                            Contract();
                            n = this.entries.Length;
                        }

                        return null;
                    }

                    if (k == 0) break;
                    i++;
                    if (i >= n) i = 0;
                    k = entries[i].Key;
                }

                return result;
            }
            set
            {
                if (key <= 0) throw new ArgumentException(ExceptionStrings.KeyNeedsToBeGreaterThanZero, "key");
                var entries = this.entries;
                var n = entries.Length;
                var i = key & (n - 1);
                var k = entries[i].Key;
                while (true)
                {
                    if (k == key || k == 0)
                    {
                        if (value == null)
                            entries[i].Value = null;
                        else
                            entries[i].Value = new WeakReference(value);
                        if (k == 0)
                        {
                            if (value == null) return;
                            entries[i].Key = key;
                            if (++Count > n / 2)
                            {
                                Expand(); //Could decrease this.count because of collected entries being deleted
                                while (Count < n / 4 && n > 16)
                                {
                                    Contract();
                                    n = this.entries.Length;
                                }
                            }

                            return;
                        }

                        if (value == null) entries[i].Key = -1;
                        return;
                    }

                    i++;
                    if (i >= n) i = 0;
                    k = entries[i].Key;
                }
            }
        }

        private void Expand()
        {
            var oldEntries = this.entries;
            var n = oldEntries.Length;
            var m = n * 2;
            if (m <= 0) return;
            var entries = new HashEntry[m];
            var count = 0;
            for (var i = 0; i < n; i++)
            {
                var key = oldEntries[i].Key;
                if (key <= 0) continue; //No entry (0) or deleted entry (-1)
                var value = oldEntries[i].Value;
                Debug.Assert(value != null);
                if (value == null || !value.IsAlive) continue; //Collected entry.
                var j = key & (m - 1);
                var k = entries[j].Key;
                while (true)
                {
                    if (k == 0)
                    {
                        entries[j].Value = value;
                        entries[j].Key = key;
                        count++;
                        break;
                    }

                    j++;
                    if (j >= m) j = 0;
                    k = entries[j].Key;
                }
            }

            this.entries = entries;
            Count = count;
        }

        private void Contract()
        {
            var oldEntries = this.entries;
            var n = oldEntries.Length;
            var m = n / 2;
            if (m < 16) return;
            var entries = new HashEntry[m];
            var count = 0;
            for (var i = 0; i < n; i++)
            {
                var key = oldEntries[i].Key;
                if (key <= 0) continue; //No entry (0) or deleted entry (-1)
                var value = oldEntries[i].Value;
                Debug.Assert(value != null);
                if (value == null || !value.IsAlive) continue; //Collected entry.
                var j = key & (m - 1);
                var k = entries[j].Key;
                while (true)
                {
                    if (k == 0)
                    {
                        entries[j].Value = value;
                        entries[j].Key = key;
                        count++;
                        break;
                    }

                    j++;
                    if (j >= m) j = 0;
                    k = entries[j].Key;
                }
            }

            this.entries = entries;
            Count = count;
        }

        private void WeedOutCollectedEntries()
        {
            var oldEntries = this.entries;
            var n = oldEntries.Length;
            var entries = new HashEntry[n];
            var count = 0;
            for (var i = 0; i < n; i++)
            {
                var key = oldEntries[i].Key;
                if (key <= 0) continue; //No entry (0) or deleted entry (-1)
                var value = oldEntries[i].Value;
                Debug.Assert(value != null);
                if (value == null || !value.IsAlive) continue; //Collected entry.
                var j = key & (n - 1);
                var k = entries[j].Key;
                while (true)
                {
                    if (k == 0)
                    {
                        entries[j].Value = value;
                        entries[j].Key = key;
                        count++;
                        break;
                    }

                    j++;
                    if (j >= n) j = 0;
                    k = entries[j].Key;
                }
            }

            this.entries = entries;
            Count = count;
        }

        public TrivialHashtableUsingWeakReferences Clone()
        {
            var clonedEntries = (HashEntry[])entries.Clone();
            //^ assume clonedEntries != null;
            return new TrivialHashtableUsingWeakReferences(clonedEntries, Count);
        }

        private struct HashEntry
        {
            public int Key;
            public WeakReference Value;
        }
    }

    public interface IUniqueKey
    {
        int UniqueId { get; }
    }

    /// <summary>
    ///     A node in an Abstract Syntax Tree.
    /// </summary>
    public abstract class Node : IUniqueKey
    {
#if !MinimalReader
        public bool IsErroneous;
#endif
        /// <summary>
        ///     The region in the source code that contains the concrete syntax corresponding to this node in the Abstract Syntax
        ///     Tree.
        /// </summary>
#if !FxCop
#if DEBUG
        public virtual SourceContext SourceContext { get; set; }
#else
    public SourceContext SourceContext;
#endif
#else
    internal SourceContext sourceContext;
    public SourceContext SourceContext {
      get{return this.sourceContext;}
      internal set{this.sourceContext = value;}
    }
#endif
#if DEBUG && !MinimalReader
        public string DebugLabel; // useful for debugging.
#endif
        protected Node(NodeType nodeType)
        {
            NodeType = nodeType;
        }

        /// <summary>
        ///     A scalar tag that identifies the concrete type of the node. This is provided to allow efficient type membership
        ///     tests that
        ///     facilitate tree traversal.
        /// </summary>
        public NodeType NodeType { get; set; }

        private static int uniqueKeyCounter;
        private int uniqueKey;

        /// <summary>
        ///     An integer that uniquely identifies this node. This provides an efficient equality test to facilitate hashing.
        ///     Do not override this.
        /// </summary>
        public virtual int UniqueKey
        {
            get
            {
                if (uniqueKey == 0)
                {
                    TryAgain:
                    var c = uniqueKeyCounter;
                    var cp1 = c + 17;
                    if (cp1 <= 0) cp1 = 1000000;
                    if (Interlocked.CompareExchange(ref uniqueKeyCounter, cp1, c) != c) goto TryAgain;
                    uniqueKey = cp1;
                }

                return uniqueKey;
            }
        }

        /// <summary>
        ///     Makes a shallow copy of the node.
        /// </summary>
        /// <returns>A shallow copy of the node</returns>
        public virtual Node /*!*/ Clone()
        {
            var result = (Node)MemberwiseClone();
            result.uniqueKey = 0;
            return result;
        }
#if !MinimalReader
        public virtual object GetVisitorFor(object /*!*/ callingVisitor, string /*!*/ visitorClassName)
        {
            if (callingVisitor == null || visitorClassName == null)
            {
                Debug.Fail("");
                return null;
            }

            return GetVisitorFor(GetType(), callingVisitor, visitorClassName);
        }

        private static Hashtable VisitorTypeFor; //contains weak references
        private static object GetVisitorFor(Type /*!*/ nodeType, object /*!*/ callingVisitor,
            string /*!*/ visitorClassName)
        {
            if (nodeType == null || callingVisitor == null || visitorClassName == null)
            {
                Debug.Fail("");
                return null;
            }

            if (VisitorTypeFor == null) VisitorTypeFor = new Hashtable();
            var customVisitorClassName = visitorClassName;
            if (visitorClassName.IndexOf('.') < 0) customVisitorClassName = nodeType.Namespace + "." + visitorClassName;
            if (customVisitorClassName == callingVisitor.GetType().FullName)
            {
                Debug.Assert(
                    false); //This must be a bug, the calling visitor is the one that should handle the nodeType
                return null;
            }

            Reflection.AssemblyName visitorAssemblyName = null;
            Assembly assembly = null;
            var wref = (WeakReference)VisitorTypeFor[customVisitorClassName];
            var visitorType = wref == null ? null : (Type)wref.Target;
            if (visitorType == typeof(object)) return null;
            string callerDirectory = null;
            if (visitorType == null)
            {
                assembly = nodeType.Assembly;
                if (assembly == null) return null;
                visitorType = assembly.GetType(customVisitorClassName, false);
            }

            if (visitorType == null)
            {
                //^ assert assembly != null;
                if (assembly.Location == null) return null;
                callerDirectory = Path.GetDirectoryName(assembly.Location);
                visitorAssemblyName = new Reflection.AssemblyName();
                visitorAssemblyName.Name = "Visitors";
                visitorAssemblyName.CodeBase = "file:///" + Path.Combine(callerDirectory, "Visitors.dll");
                try
                {
                    assembly = Assembly.Load(visitorAssemblyName);
                }
                catch
                {
                }

                if (assembly != null)
                    visitorType = assembly.GetType(customVisitorClassName, false);
                if (visitorType == null)
                {
                    visitorAssemblyName.Name = customVisitorClassName;
                    visitorAssemblyName.CodeBase =
                        "file:///" + Path.Combine(callerDirectory, customVisitorClassName + ".dll");
                    try
                    {
                        assembly = Assembly.Load(visitorAssemblyName);
                    }
                    catch
                    {
                    }

                    if (assembly != null)
                        visitorType = assembly.GetType(customVisitorClassName, false);
                }
            }

            if (visitorType == null)
            {
                //Put fake entry into hashtable to short circuit future lookups
                visitorType = typeof(object);
                assembly = nodeType.Assembly;
            }

            if (assembly != null) //Only happens if there was a cache miss
                lock (VisitorTypeFor)
                {
                    VisitorTypeFor[customVisitorClassName] = new WeakReference(visitorType);
                }

            if (visitorType == typeof(object)) return null;
            try
            {
                return Activator.CreateInstance(visitorType, callingVisitor);
            }
            catch
            {
            }

            return null;
        }
#endif
        int IUniqueKey.UniqueId => UniqueKey;
#if MinimalReader
    // Return a constant value for IsNormalized in the binary-only
    // reader. This results in less code churn elsewhere.
    internal bool IsNormalized{get{return true;}}
#endif
    }
#if !MinimalReader && !CodeContracts
  public abstract class ErrorNode : Node{
    public int Code;
#if ExtendedRuntime
    public bool DoNotSuppress;
#endif
    public string[] MessageParameters;

    protected ErrorNode(int code, params string[] messageParameters)
      : base(NodeType.Undefined){
      this.Code = code;
      this.MessageParameters = messageParameters;
    }
    public virtual string GetErrorNumber(){
      return this.Code.ToString("0000");
    }
    public string GetMessage(){
      return this.GetMessage(null);
    }

    public abstract string GetMessage(System.Globalization.CultureInfo culture);
    public virtual string GetMessage(string key, System.Resources.ResourceManager rm, System.Globalization.CultureInfo culture){
      if (rm == null || key == null) return null;
      string localizedString = rm.GetString(key, culture);
      if (localizedString == null) localizedString = key;
      string[] messageParameters = this.MessageParameters;
      if (messageParameters == null || messageParameters.Length == 0) return localizedString;
      return string.Format(localizedString, messageParameters);
    }
    public abstract int Severity{
      get;
    }
    public static int GetCountAtSeverity(ErrorNodeList errors, int minSeverity, int maxSeverity){
      if (errors == null) return 0;
      int n = 0;
      for (int i = 0; i < errors.Count; i++){
        ErrorNode e = errors[i];
        if (e == null)
          continue;
        int s = e.Severity;
        if (minSeverity <= s && s <= maxSeverity)
          n++;
      }
      return n;
    }
  }
  public class Expose : Statement{
    public Expression Instance;
    public Block Body;
    public bool IsLocal;
    public Expose(NodeType nodeType)
      : base(nodeType){
    }
  }
  public class Acquire : Statement{
    public bool ReadOnly;
    public Statement Target;
    public Expression Condition;
    public Expression ConditionFunction;
    public Block Body;
    public BlockScope ScopeForTemporaryVariable;
    public Acquire() : base(NodeType.Acquire) {
    }
  }
#endif
    public class Expression : Node
    {
        private TypeNode type;
#if FxCop
    internal int ILOffset;
#endif
#if !FxCop && ILOFFSETS
        public int ILOffset;
#endif
        public Expression(NodeType nodeType)
            : base(nodeType)
        {
        }

        public Expression(NodeType nodeType, TypeNode type)
            : base(nodeType)
        {
            this.type = type;
        }

        public virtual TypeNode Type
        {
            get { return type; }
            set { type = value; }
        }
    }
#if !MinimalReader && !CodeContracts
  public class ExpressionSnippet : Expression{
    public IParserFactory ParserFactory;

    public ExpressionSnippet()
      : base(NodeType.ExpressionSnippet){
    }
    public ExpressionSnippet(IParserFactory parserFactory, SourceContext sctx)
      : base(NodeType.ExpressionSnippet){
      this.ParserFactory = parserFactory;
      this.SourceContext = sctx;
    }
  }
#endif
    public class MemberBinding : Expression
    {
        private Member boundMember;
#if !MinimalReader
        public Expression BoundMemberExpression;
#endif

        public MemberBinding()
            : base(NodeType.MemberBinding)
        {
        }

        public MemberBinding(Expression targetObject, Member /*!*/ boundMember)
            : this(targetObject, boundMember, false, -1)
        {
            if (boundMember is Field) Volatile = ((Field)boundMember).IsVolatile;
        }

        public MemberBinding(Expression targetObject, Member /*!*/ boundMember, bool @volatile, int alignment)
            : base(NodeType.MemberBinding)
        {
            Contract.Requires(boundMember != null);

            Alignment = alignment;
            this.boundMember = boundMember;
            TargetObject = targetObject;
            Volatile = @volatile;
            switch (boundMember.NodeType)
            {
                case NodeType.Field:
                    Type = ((Field)boundMember).Type;
                    break;
                case NodeType.Method:
                    Type = ((Method)boundMember).ReturnType;
                    break;
                case NodeType.Event:
                    Type = ((Event)boundMember).HandlerType;
                    break;
                default:
                    Type = boundMember as TypeNode;
                    break;
            }
        }

        public int Alignment { get; set; }

        public Member BoundMember
        {
            get { return boundMember; }
            set
            {
#if CLOUSOT || CodeContracts
                Contract.Requires(value != null);
#endif
                boundMember = value;
            }
        }

        public Expression TargetObject { get; set; }

        public bool Volatile { get; set; }
#if !MinimalReader
        public MemberBinding(Expression targetObject, Member /*!*/ boundMember, Expression boundMemberExpression)
            : this(targetObject, boundMember, false, -1)
        {
            if (boundMember is Field) Volatile = ((Field)boundMember).IsVolatile;
            BoundMemberExpression = boundMemberExpression;
        }

        public MemberBinding(Expression targetObject, Member /*!*/ boundMember, SourceContext sctx)
            : this(targetObject, boundMember, false, -1)
        {
            if (boundMember is Field) Volatile = ((Field)boundMember).IsVolatile;
            SourceContext = sctx;
        }

        public MemberBinding(Expression targetObject, Member /*!*/ boundMember, SourceContext sctx,
            Expression boundMemberExpression)
            : this(targetObject, boundMember, false, -1)
        {
            if (boundMember is Field) Volatile = ((Field)boundMember).IsVolatile;
            SourceContext = sctx;
            BoundMemberExpression = boundMemberExpression;
        }
#endif
    }

    public class AddressDereference : Expression
    {
        public AddressDereference()
            : base(NodeType.AddressDereference)
        {
        }

        public AddressDereference(Expression address, TypeNode type)
            : this(address, type, false, -1)
        {
        }
#if !MinimalReader
        public AddressDereference(Expression address, TypeNode type, SourceContext sctx)
            : this(address, type, false, -1, sctx)
        {
        }
#endif
        public AddressDereference(Expression address, TypeNode type, bool isVolatile, int alignment)
            : base(NodeType.AddressDereference)
        {
            Address = address;
            Alignment = alignment;
            Type = type;
            Volatile = isVolatile;
        }
#if !MinimalReader
        public AddressDereference(Expression address, TypeNode type, bool Volatile, int alignment, SourceContext sctx)
            : base(NodeType.AddressDereference)
        {
            Address = address;
            Alignment = alignment;
            Type = type;
            this.Volatile = Volatile;
            SourceContext = sctx;
        }
#endif
        public Expression Address { get; set; }

        public int Alignment { get; set; }

        public bool Volatile { get; set; }
#if !MinimalReader
        public enum ExplicitOp
        {
            None = 0,
            Star,
            Arrow
        }

#endif
#if !MinimalReader
        public bool Explicit => ExplicitOperator != ExplicitOp.None;

        public ExplicitOp ExplicitOperator { get; set; }
#endif
    }

    public class UnaryExpression : Expression
    {
        public UnaryExpression()
            : base(NodeType.Nop)
        {
        }

        public UnaryExpression(Expression operand, NodeType nodeType)
            : base(nodeType)
        {
            Operand = operand;
        }
#if !MinimalReader
        public UnaryExpression(Expression operand, NodeType nodeType, SourceContext sctx)
            : base(nodeType)
        {
            Operand = operand;
            SourceContext = sctx;
        }
#endif
        public UnaryExpression(Expression operand, NodeType nodeType, TypeNode type)
            : base(nodeType)
        {
            Operand = operand;
            Type = type;
        }
#if !MinimalReader
        public UnaryExpression(Expression operand, NodeType nodeType, TypeNode type, SourceContext sctx)
            : base(nodeType)
        {
            Operand = operand;
            Type = type;
            SourceContext = sctx;
        }
#endif
        public Expression Operand { get; set; }
    }
#if !MinimalReader
    public class PrefixExpression : Expression
    {
        public Expression Expression;
        public NodeType Operator;
        public Method OperatorOverload;

        public PrefixExpression()
            : base(NodeType.PrefixExpression)
        {
        }

        public PrefixExpression(Expression expression, NodeType Operator, SourceContext sourceContext)
            : base(NodeType.PrefixExpression)
        {
            Expression = expression;
            this.Operator = Operator;
            SourceContext = sourceContext;
        }
    }

    public class PostfixExpression : Expression
    {
        public Expression Expression;
        public NodeType Operator;
        public Method OperatorOverload;

        public PostfixExpression()
            : base(NodeType.PostfixExpression)
        {
        }

        public PostfixExpression(Expression expression, NodeType Operator, SourceContext sourceContext)
            : base(NodeType.PostfixExpression)
        {
            Expression = expression;
            this.Operator = Operator;
            SourceContext = sourceContext;
        }
    }
#endif
    public class BinaryExpression : Expression
    {
        public BinaryExpression()
            : base(NodeType.Nop)
        {
        }

        public BinaryExpression(Expression operand1, Expression operand2, NodeType nodeType)
            : base(nodeType)
        {
            Operand1 = operand1;
            Operand2 = operand2;
        }

        public BinaryExpression(Expression operand1, Expression operand2, NodeType nodeType, TypeNode resultType)
            : base(nodeType)
        {
            Operand1 = operand1;
            Operand2 = operand2;
            Type = resultType;
        }

        public Expression Operand1 { get; set; }

        public Expression Operand2 { get; set; }
#if !MinimalReader
        public BinaryExpression(Expression operand1, Expression operand2, NodeType nodeType, SourceContext ctx)
            : base(nodeType)
        {
            Operand1 = operand1;
            Operand2 = operand2;
            SourceContext = ctx;
        }

        public BinaryExpression(Expression operand1, Expression operand2, NodeType nodeType, TypeNode resultType,
            SourceContext ctx)
            : base(nodeType)
        {
            Operand1 = operand1;
            Operand2 = operand2;
            Type = resultType;
            SourceContext = ctx;
        }
#endif
    }

    public class TernaryExpression : Expression
    {
        public TernaryExpression()
            : base(NodeType.Nop)
        {
        }

        public TernaryExpression(Expression operand1, Expression operand2, Expression operand3, NodeType nodeType,
            TypeNode resultType)
            : base(nodeType)
        {
            Operand1 = operand1;
            Operand2 = operand2;
            Operand3 = operand3;
            Type = resultType;
        }

        public Expression Operand1 { get; set; }

        public Expression Operand2 { get; set; }

        public Expression Operand3 { get; set; }
    }

    public abstract class NaryExpression : Expression
    {
#if !FxCop
        public ExpressionList Operands;
#else
    private ExpressionList operands;
    public ExpressionList Operands {
      get {return this.operands;}
      internal set{this.operands = value;}
    }
#endif
        protected NaryExpression()
            : base(NodeType.Nop)
        {
        }

        protected NaryExpression(ExpressionList operands, NodeType nodeType)
            : base(nodeType)
        {
            Operands = operands;
        }
    }
#if !MinimalReader
    public class ApplyToAll : BinaryExpression
    {
        public Local ElementLocal;
        public Method ResultIterator;

        public ApplyToAll()
            : base(null, null, NodeType.ApplyToAll)
        {
        }

        public ApplyToAll(Expression operand1, Expression operand2)
            : base(operand1, operand2, NodeType.ApplyToAll)
        {
        }

        public ApplyToAll(Expression operand1, Expression operand2, SourceContext ctx)
            : base(operand1, operand2, NodeType.ApplyToAll)
        {
            SourceContext = ctx;
        }
    }
#endif
    public class NamedArgument : Expression
    {
        public NamedArgument()
            : base(NodeType.NamedArgument)
        {
        }

        public NamedArgument(Identifier name, Expression value)
            : base(NodeType.NamedArgument)
        {
            Name = name;
            Value = value;
        }
#if !MinimalReader
        public NamedArgument(Identifier name, Expression value, SourceContext ctx)
            : base(NodeType.NamedArgument)
        {
            Name = name;
            Value = value;
            SourceContext = ctx;
        }
#endif
        public bool IsCustomAttributeProperty
        {
            //TODO: rename this to IsProperty
            get;
            set;
        }

        public Identifier Name { get; set; }

        public Expression Value { get; set; }

        public bool ValueIsBoxed { get; set; }
    }

    /// <summary>
    ///     This an Expression wrapper for compile time constants. It is assumed to be correct by construction.
    ///     In Normalized IR, the wrapped value must be a primitive numeric type or an enum or a string or null.
    ///     If used in custom attributes, types are also allowed as well as single dimensional arrays of other allowed types.
    ///     If the wrapped value is null, any reference type is allowed, except in custom attributes, where it must be Type or
    ///     String.
    /// </summary>
    public class Literal : Expression
    {
        public Literal()
            : base(NodeType.Literal)
        {
        }
#if !NoReflection
        public Literal(object Value)
            : base(NodeType.Literal)
        {
            this.Value = Value;
        }
#endif
        public Literal(object value, TypeNode type)
            : base(NodeType.Literal)
        {
            Value = value;
            Type = type;
        }

        public Literal(object value, TypeNode type, SourceContext sourceContext)
            : base(NodeType.Literal)
        {
            Value = value;
            SourceContext = sourceContext;
            Type = type;
        }

        /// <summary>
        ///     Holds the wrapped compile time constant value.
        /// </summary>
        public object Value { get; }

        public override string ToString()
        {
            if (Value == null) return "Literal for null";
            return Value.ToString();
        }
#if !MinimalReader
        public bool TypeWasExplicitlySpecifiedInSource;
        public Expression SourceExpression;
#endif
#if !NoWriter
        public static bool IsNullLiteral(Expression expr)
        {
            var lit = expr as Literal;
            if (lit == null) return false;
            if (lit.Type != CoreSystemTypes.Object || lit.Value != null) return false;
            return true;
        }

        //TODO: replace these with properties that freshly allocate them. It appears that Literals sometimes get clobbered.
        public static Literal DoubleOne;
        public static Literal False;
        public static Literal Int32MinusOne;
        public static Literal Int32Zero;
        public static Literal Int32One;
        public static Literal Int32Two;
        public static Literal Int32Sixteen;
        public static Literal Int64Zero;
        public static Literal Int64One;
        public static Literal Null;
        public static Literal SingleOne;
        public static Literal True;

        public static void Initialize()
        {
            DoubleOne = new Literal(1.0, CoreSystemTypes.Double);
            False = new Literal(false, CoreSystemTypes.Boolean);
            Int32MinusOne = new Literal(-1, CoreSystemTypes.Int32);
            Int32Zero = new Literal(0, CoreSystemTypes.Int32);
            Int32One = new Literal(1, CoreSystemTypes.Int32);
            Int32Two = new Literal(2, CoreSystemTypes.Int32);
            Int32Sixteen = new Literal(16, CoreSystemTypes.Int32);
            Int64Zero = new Literal(0L, CoreSystemTypes.Int64);
            Int64One = new Literal(1L, CoreSystemTypes.Int64);
            Null = new Literal(null, CoreSystemTypes.Object);
            SingleOne = new Literal(1.0f, CoreSystemTypes.Single);
            True = new Literal(true, CoreSystemTypes.Boolean);
        }

        public static void ClearStatics()
        {
            DoubleOne = null;
            False = null;
            Int32MinusOne = null;
            Int32Zero = null;
            Int32One = null;
            Int32Two = null;
            Int32Sixteen = null;
            Int64Zero = null;
            Int64One = null;
            Null = null;
            SingleOne = null;
            True = null;
        }
#endif
    }

    public class This : Parameter
    {
        public This()
        {
            NodeType = NodeType.This;
            Name = StandardIds.This;
        }

        public This(TypeNode type)
        {
            NodeType = NodeType.This;
            Name = StandardIds.This;
            Type = type;
        }
#if !MinimalReader && !CodeContracts
    public bool IsCtorCall = false;
    public This(SourceContext sctx, bool isCtorCall){
      this.NodeType = NodeType.This;
      this.Name = StandardIds.This;
      this.SourceContext = sctx;
      this.IsCtorCall = isCtorCall;
    }
    public This(TypeNode type, SourceContext sctx){
      this.NodeType = NodeType.This;
      this.Name = StandardIds.This;
      this.Type = type;
      this.SourceContext = sctx;
    }
    public override bool Equals(object obj) {
      ThisBinding binding = obj as ThisBinding;
      return obj == this || binding != null && binding.BoundThis == this;
    }
    public override int GetHashCode(){
      return base.GetHashCode();
    }
#endif
#if ExtendedRuntime
    public override bool IsUniversallyDelayed {
      get {
        if (this.DeclaringMethod is InstanceInitializer && this.DeclaringMethod.DeclaringType != null &&
            !this.DeclaringMethod.DeclaringType.IsValueType) {
          // by default, class constructors should be delayed
          return !(this.DeclaringMethod.GetAttribute(ExtendedRuntimeTypes.NotDelayedAttribute) != null);
        }
        return (this.DeclaringMethod.GetAttribute(ExtendedRuntimeTypes.DelayedAttribute) != null);
      }
    }

#endif
    }
#if !MinimalReader && !CodeContracts
  public class ThisBinding : This, IUniqueKey{
    public This/*!*/ BoundThis;
    public ThisBinding(This/*!*/ boundThis, SourceContext sctx) {
      if (boundThis == null) throw new ArgumentNullException("boundThis");
      this.BoundThis = boundThis;
      this.SourceContext = sctx;
      this.Type = boundThis.Type;
      this.Name = boundThis.Name;
      this.TypeExpression = boundThis.TypeExpression;
      this.Attributes = boundThis.Attributes;
      this.DefaultValue = boundThis.DefaultValue;
      this.Flags = boundThis.Flags;
      this.MarshallingInformation = boundThis.MarshallingInformation;
      this.DeclaringMethod = boundThis.DeclaringMethod;
      this.ParameterListIndex = boundThis.ParameterListIndex;
      this.ArgumentListIndex = boundThis.ArgumentListIndex;
      //^ base();
    }
    public override int GetHashCode(){
      return this.BoundThis.GetHashCode();
    }
    public override bool Equals(object obj){
      ThisBinding pb = obj as ThisBinding;
      if (pb != null)
        return this.BoundThis.Equals(pb.BoundThis);
      else
        return this.BoundThis.Equals(obj);
    }
    int IUniqueKey.UniqueId{
      get {return this.BoundThis.UniqueKey;}
    }
    /// <summary>
    /// Must forward type to underlying binding, since ThisBindings get built at times when
    /// the bound This node does not have its final type yet.
    /// </summary>
    public override TypeNode Type {
      get {
        return BoundThis.Type;
      }
      set {
        BoundThis.Type = value;
      }
    }
  }
  public class Base : Expression{
    /// <summary>
    /// When the source uses the C# compatibility mode, base calls cannot be put after non-null
    /// field initialization, but must be put before the body. But the user can specify where
    /// the base ctor call should be performed by using "base;" as a marker. During parsing
    /// this flag is set so the right code transformations can be performed at code generation.
    /// </summary>
    public bool UsedAsMarker;
    public bool IsCtorCall = false;
    public Base()
      : base(NodeType.Base){
    }
    public Base(SourceContext sctx, bool isCtorCall)
      : base(NodeType.Base){
      this.SourceContext = sctx;
      this.IsCtorCall = isCtorCall;
    }
  }
  public class ImplicitThis : Expression{
    public int LexLevel;
    public Class MostNestedScope;
    public ImplicitThis()
      : base(NodeType.ImplicitThis){
    }
    public ImplicitThis(Class mostNestedScope, int lexLevel)
      : base(NodeType.ImplicitThis){
      this.LexLevel = lexLevel;
      this.MostNestedScope = mostNestedScope;
    }
  }
  public class CurrentClosure : Expression{
    public Method Method;
    public CurrentClosure()
      : base(NodeType.CurrentClosure){
    }
    public CurrentClosure(Method method, TypeNode type)
      : base(NodeType.CurrentClosure){
      this.Method = method;
      this.Type = type;
    }
    public CurrentClosure(Method method, TypeNode type, SourceContext sctx)
      : base(NodeType.CurrentClosure){
      this.Method = method;
      this.Type = type;
      this.SourceContext = sctx;
    }
  }
  public class SetterValue : Expression{
    public SetterValue()
      : base(NodeType.SetterValue){
    }
  }
#endif
    public class Identifier : Expression
    {
        private readonly int hashCode;
        internal readonly int length;
#if CodeContracts || FxCop
        readonly
#endif
            private string name;

        private readonly int offset;
#if !FxCop && !CodeContracts
    private DocumentText text;
#endif
#if !MinimalReader
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public readonly Identifier Prefix;
#endif
        /// <summary>An identifier with the empty string ("") as its value.</summary>
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Identifier /*!*/
            Empty = new Identifier(""); // Should be thread-safe
#if !FxCop && !CodeContracts
    private Identifier(DocumentText/*!*/ text, int offset, int length)
      : base(NodeType.Identifier){
      this.text = text;
      this.offset = offset;
      this.length = length;
      ulong hcode = 0;
      for (int i = offset, n = length+i; i < n; i++){
        char ch = text[i];
        hcode = hcode*17 + ch;
      }
      this.hashCode = ((int)hcode) & int.MaxValue;
    }
    public static Identifier/*!*/ For(SourceContext sctx) {
      DocumentText text = null;
      if (sctx.Document != null) text = sctx.Document.Text;
      if (text == null) text = new DocumentText("");
      Identifier id = new Identifier(text, sctx.StartPos, sctx.EndPos-sctx.StartPos);
      id.SourceContext = sctx;
      return id;
    }
#endif
        public Identifier(string name)
            : base(NodeType.Identifier)
        {
            if (name == null) name = "";
            this.name = name;
            var n = length = name.Length;
            ulong hcode = 0;
            for (var i = 0; i < n; i++)
            {
                var ch = name[i];
                hcode = hcode * 17 + ch;
            }

            hashCode = (int)hcode & int.MaxValue;
        }
#if !MinimalReader
        public Identifier(string name, SourceContext sctx)
            : this(name)
        {
            SourceContext = sctx;
        }
#endif
        public static Identifier /*!*/ For(string /*!*/ name)
        {
            return new Identifier(name);
        }

        private unsafe Identifier(byte* pointer, int offset)
            : base(NodeType.Identifier)
        {
            this.offset = offset;
            int length;
            ulong hcode;
            var isASCII = ComputeHash(pointer, offset, out length, out hcode);
            if (isASCII)
            {
                hashCode = (int)hcode & int.MaxValue;
                this.length = length;
                this.name = new string((sbyte*)pointer, offset, length, Encoding.ASCII);
                return;
            }

            hcode = 0;
            var name = this.name = new string((sbyte*)pointer, offset, length, Encoding.UTF8);
            for (int i = 0, n = this.length = name.Length; i < n; i++)
            {
                var ch = name[i];
                hcode = hcode * 17 + ch;
            }

            hashCode = (int)hcode & int.MaxValue;
        }

        private static unsafe bool ComputeHash(byte* pointer, int offset, out int length, out ulong hcode)
        {
            length = 0;
            hcode = 0;
            var isASCII = true;
            for (var i = offset;; i++)
            {
                var b = *(pointer + i);
                if (b == 0) break;
                if ((b & 0x80) != 0) isASCII = false;
                hcode = hcode * 17 + b;
                length++;
            }

            return isASCII;
        }

        /// <summary>
        ///     Use when pointer+offset points to a null terminated string of UTF8 code points.
        /// </summary>
        internal static unsafe Identifier /*!*/ For(byte* pointer, int offset)
        {
            //TODO: first look for identifier in cache
#if CodeContracts
            int length;
            ulong hcode;
            Identifier result;
            var isASCII = ComputeHash(pointer, offset, out length, out hcode);
            if (isASCII)
            {
                result = TryHashLookup(pointer, offset, length, (int)hcode & int.MaxValue);
                if (result != null) return result;
            }

            result = new Identifier(pointer, offset);
            var uniqueKey = result.UniqueIdKey; // force hashtable entry
            return result;
#else
      return new Identifier(pointer, offset);
#endif
        }

        private unsafe Identifier(byte* pointer, uint length)
            : base(NodeType.Identifier)
        {
            //this.offset = 0;
            this.length = (int)length;
            ulong hcode = 0;
            for (uint i = 0; i < length; i++)
            {
                var b = *(pointer + i);
                if ((b & 0x80) != 0) goto doUTF8decoding;
                hcode = hcode * 17 + b;
            }

            hashCode = (int)hcode & int.MaxValue;
            this.name = new string((sbyte*)pointer, 0, this.length, Encoding.ASCII);
            return;
            doUTF8decoding:
            var name = this.name = new string((sbyte*)pointer, 0, this.length, Encoding.UTF8);
            for (int i = 0, n = this.length = name.Length; i < n; i++)
            {
                var ch = name[i];
                hcode = hcode * 17 + ch;
            }

            hashCode = (int)hcode & int.MaxValue;
        }

        /// <summary>
        ///     Use when pointer points to a string of UTF8 code points of a given length
        /// </summary>
        internal static unsafe Identifier /*!*/ For(byte* pointer, uint length)
        {
            //TODO: first look for identifier in cache
            return new Identifier(pointer, length);
        }

        private static readonly object /*!*/
            Lock = new object();

        private struct CanonicalIdentifier
        {
#if CodeContracts
            internal readonly Identifier id;
            internal string Name => id == null ? null : id.Name;
#else
      internal string/*!*/ Name;
#endif
            internal readonly int UniqueIdKey;
            internal readonly int HashCode;

            internal CanonicalIdentifier(
#if CodeContracts
                Identifier name
#else
        string/*!*/ name
#endif
                , int uniqueIdKey, int hashCode
            )
            {
#if CodeContracts
                id = name;
#else
        this.Name = name;
#endif
                UniqueIdKey = uniqueIdKey;
                HashCode = hashCode;
            }
        }

        private static CanonicalIdentifier[] /*!*/
            HashTable = new CanonicalIdentifier[16 * 1024];

        private static int count;

        private int GetUniqueIdKey()
        {
            lock (Lock)
            {
                var hcode = hashCode;
                var hTable = HashTable;
                var length = hTable.Length;
                var i = hcode % length;
                var id = hTable[i];
                while (id.Name != null)
                {
                    if (HasSameNameAs(id)) return id.UniqueIdKey;
                    i = (i + 1) % length;
                    id = hTable[i];
                }

                var count = Identifier.count;
                var countp1 = count + 1;
                Identifier.count = countp1;
                var name = Name; //Get a local copy of the name and drop any reference to a DocumentText instance
#if !CodeContracts
        hTable[i] = new CanonicalIdentifier(name, countp1, hcode);
#else
                hTable[i] = new CanonicalIdentifier(this, countp1, hcode);
#endif
                if (countp1 > length / 2) Rehash(); //Threshold exceeded, need to rehash        
                return countp1;
            }
        }

#if CodeContracts
        private static unsafe Identifier TryHashLookup(byte* ptr, int offset, int slen, int hcode)
        {
            lock (Lock)
            {
                var hTable = HashTable;
                var length = hTable.Length;
                var i = hcode % length;
                var id = hTable[i];
                while (id.Name != null)
                {
                    if (HasSameNameAs(id.Name, ptr, offset, slen)) return id.id;
                    i = (i + 1) % length;
                    id = hTable[i];
                }

                return null;
            }
        }

        private static unsafe bool HasSameNameAs(string name, byte* ptr, int offset, int slen)
        {
            Contract.Requires(name != null);

            if (slen != name.Length) return false;
            for (var i = 0; i < slen; i++)
                if ((short)name[i] != ptr[offset++])
                    return false;
            return true;
        }
#endif

        private bool HasSameNameAs(CanonicalIdentifier id)
        {
            var myLength = length;
            var idLength = id.Name.Length;
            if (myLength != idLength) return false;
            var myName = name;
            var idName = id.Name;
#if !FxCop && !CodeContracts
      if (myName == null){
        int myOffset = this.offset;
        if (this.text != null && this.text.Equals(idName, myOffset, myLength)){
          this.name = idName;
          this.text = null;
          return true;
        }
        return false;
      }
#endif
            return myName == idName;
        }

        public string /*!*/ Name
        {
            //TODO: need a better name for this property
            get
            {
#if !FxCop && !CodeContracts
        if (this.name != null) return this.name;
        lock(this){
          if (this.name != null) return this.name;
          //^ assume this.text != null;
          int length = this.length;
          int offset = this.offset;
          this.name = this.text.Substring(offset, length);
          this.text = null;
          return this.name;
        }
#else
                return name;
#endif
            }
        }

        private static void Rehash()
        {
            var hTable = HashTable;
            var n = hTable.Length;
            var n2 = n * 2;
            var newhTable = new CanonicalIdentifier[n2];
            for (var i = 0; i < n; i++)
            {
                var id = hTable[i];
                if (id.Name == null) continue;
                var j = id.HashCode % n2;
                var id2 = newhTable[j];
                while (id2.Name != null)
                {
                    j = (j + 1) % n2;
                    id2 = newhTable[j];
                }

                newhTable[j] = id;
            }

            HashTable = newhTable;
        }

        public override string /*!*/ ToString()
        {
#if !MinimalReader
            if (Prefix != null)
                return Prefix.Name + ":" + Name;
#endif
            if (Name == null) return "";
            return Name;
        }

        private int uniqueIdKey;

        /// <summary>
        ///     Returns an integer that is the same for every Identifier instance that has the same string value, and that is
        ///     different from
        ///     every other identifier instance that has a different string value. Useful for efficient equality tests when hashing
        ///     identifiers.
        /// </summary>
        public int UniqueIdKey
        {
            get
            {
                var result = uniqueIdKey;
                if (result != 0) return result;
                return uniqueIdKey = GetUniqueIdKey();
            }
        }

        [Obsolete("Use Identifier.UniqueIdKey instead")]
        public new int UniqueKey
        {
            get
            {
                var result = uniqueIdKey;
                if (result != 0) return result;
                return uniqueIdKey = GetUniqueIdKey();
            }
        }
    }
#if !MinimalReader && !CodeContracts
  public class QualifiedIdentifier : Expression{
    public Identifier Identifier;
    public Expression Qualifier;
    public Expression BoundMember;
    public bool QualifierIsNamespace;

    public QualifiedIdentifier()
      : base(NodeType.QualifiedIdentifer){
    }
    public QualifiedIdentifier(Expression qualifier, Identifier identifier)
      : base(NodeType.QualifiedIdentifer){
      this.Identifier = identifier;
      this.Qualifier = qualifier;
    }
    public QualifiedIdentifier(Expression qualifier, Identifier identifier, SourceContext sourceContext)
      : base(NodeType.QualifiedIdentifer){
      this.Identifier = identifier;
      this.Qualifier = qualifier;
      this.SourceContext = sourceContext;
    }
    public QualifiedIdentifier(Expression qualifier, Identifier identifier, SourceContext sourceContext, bool qualifierIsNamespace)
      : base(NodeType.QualifiedIdentifer){
      this.Identifier = identifier;
      this.Qualifier = qualifier;
      this.SourceContext = sourceContext;
      this.QualifierIsNamespace = qualifierIsNamespace;
    }
    public override string/*!*/ ToString(){
      string str = this.Identifier == null ? "" : this.Identifier.ToString();
      if (this.Qualifier == null) return str;
      string separator = this.QualifierIsNamespace ? "::" : "+";
      return this.Qualifier.ToString()+separator+str;
    }
  }
  public class Quantifier : Expression{
    public NodeType QuantifierType;
    public TypeNode SourceType; // the type of elements the quantifier consumes
    public Comprehension Comprehension;
    public Quantifier()
      : base(NodeType.Quantifier){
    }
    public Quantifier(Comprehension comprehension)
      : base(NodeType.Quantifier){
      this.Comprehension = comprehension;
    }
    public Quantifier(NodeType t, Comprehension comprehension)
      : base(NodeType.Quantifier){
      this.QuantifierType = t;
      this.Comprehension = comprehension;
    }
  }
  public enum ComprehensionBindingMode {In, Gets};
  public class ComprehensionBinding : Expression{
    public ComprehensionBindingMode Mode = ComprehensionBindingMode.In;
    public TypeNode TargetVariableType;
    public TypeNode TargetVariableTypeExpression;
    public Expression TargetVariable;

    public TypeNode AsTargetVariableType;
    public TypeNode AsTargetVariableTypeExpression;

    public Expression SourceEnumerable;
    public BlockScope ScopeForTemporaryVariables;
    public ComprehensionBinding()
      : base(NodeType.ComprehensionBinding){
    }
  }  
  public enum ComprehensionMode {Reduction, Comprehension};
  // {1,2,3} ==> Comprehension with BindingsAndFilters = null and Elements = [1,2,3]
  // i.e., for a "display", there are no bindings and the elements have one entry per value in the comprehension
  // { int x in A, P(x); T(x); default } ==> Comprehension with BindingsAndFilters = [int x in A, P(x)] and Elements = [T(x), default]
  // i.e., for "true" comprehensions, the list of elements will always have either one or two elements (two if there is a default)
  public class Comprehension : Expression{
    public ComprehensionMode Mode = ComprehensionMode.Comprehension;
    public ExpressionList BindingsAndFilters;
    public ExpressionList Elements;

    public Node nonEnumerableTypeCtor; // used only when the comprehension should generate code for an IList, e.g.
    public Method AddMethod; // used only when the comprehension should generate code for an IList, e.g.
    public TypeNode TemporaryHackToHoldType;

    public Comprehension()
      : base(NodeType.Comprehension){
    }

    public bool IsDisplay{ 
      get{ 
        return this.BindingsAndFilters == null;
      }
    }
  }
  public class NameBinding : Expression{
    public Identifier Identifier;
    public MemberList BoundMembers;
    public Expression BoundMember;
    public int LexLevel;
    public Class MostNestedScope;
    public NameBinding()
      : base(NodeType.NameBinding){
    }
    public NameBinding(Identifier identifier, MemberList boundMembers)
      : base(NodeType.NameBinding){
      this.Identifier = identifier;
      this.BoundMembers = boundMembers;
    }
    public NameBinding(Identifier identifier, MemberList boundMembers, SourceContext sctx)
      : base(NodeType.NameBinding){
      this.Identifier = identifier;
      this.BoundMembers = boundMembers;
      this.SourceContext = sctx;
    }
    public NameBinding(Identifier identifier, MemberList boundMembers, Class mostNestedScope, int lexLevel)
      : base(NodeType.NameBinding){
      this.Identifier = identifier;
      this.BoundMembers = boundMembers;
      this.LexLevel = lexLevel;
      this.MostNestedScope = mostNestedScope;
    }
    public NameBinding(Identifier identifier, MemberList boundMembers, Class mostNestedScope, int lexLevel, SourceContext sctx)
      : base(NodeType.NameBinding) {
      this.Identifier = identifier;
      this.BoundMembers = boundMembers;
      this.LexLevel = lexLevel;
      this.MostNestedScope = mostNestedScope;
      this.SourceContext = sctx;
    }
    public override string ToString() {
      return this.Identifier == null ? "" : this.Identifier.ToString();
    }
  }
  public class TemplateInstance : Expression{
    public Expression Expression;
    public TypeNodeList TypeArguments;
    public TypeNodeList TypeArgumentExpressions;
    public bool IsMethodTemplate;
    public MemberList BoundMembers;

    public TemplateInstance()
      : this(null, null){
    }
    public TemplateInstance(Expression expression, TypeNodeList typeArguments)
      : base(NodeType.TemplateInstance){
      this.Expression = expression;
      this.TypeArguments = typeArguments;
    }
  }
  public class StackAlloc : Expression{
    public TypeNode ElementType;
    public TypeNode ElementTypeExpression;
    public Expression NumberOfElements;

    public StackAlloc()
      : base(NodeType.StackAlloc){
    }
    public StackAlloc(TypeNode elementType, Expression numberOfElements, SourceContext sctx)
      : base(NodeType.StackAlloc){
      this.ElementType = this.ElementTypeExpression = elementType;
      this.NumberOfElements = numberOfElements;
      this.SourceContext = sctx;
    }
  }
#endif
    public class MethodCall : NaryExpression
    {
        public MethodCall(Expression callee, ExpressionList arguments, NodeType typeOfCall)
            : base(arguments, typeOfCall)
        {
            Callee = callee;
#if !MinimalReader
            CalleeExpression = callee;
#endif
            //this.isTailCall = false;
        }

        public Expression Callee { get; set; }

        public bool IsTailCall { get; set; }

        public TypeNode Constraint { get; set; }
#if !MinimalReader
        public Expression CalleeExpression;
        public bool GiveErrorIfSpecialNameMethod;
        public bool ArgumentListIsIncomplete;

        public MethodCall()
        {
            NodeType = NodeType.MethodCall;
        }

        public MethodCall(Expression callee, ExpressionList arguments)
            : base(arguments, NodeType.MethodCall)
        {
            Callee = CalleeExpression = callee;
            IsTailCall = false;
        }
#endif
#if !MinimalReader
        public MethodCall(Expression callee, ExpressionList arguments, NodeType typeOfCall, TypeNode resultType)
            : this(callee, arguments, typeOfCall)
        {
            Type = resultType;
        }

        public MethodCall(Expression callee, ExpressionList arguments, NodeType typeOfCall, TypeNode resultType,
            SourceContext sctx)
            : this(callee, arguments, typeOfCall, resultType)
        {
            SourceContext = sctx;
        }
#endif
    }

    public class Construct : NaryExpression
    {
#if !MinimalReader
        public Expression Owner;
#endif
        public Construct()
        {
            NodeType = NodeType.Construct;
        }

        public Construct(Expression constructor, ExpressionList arguments)
            : base(arguments, NodeType.Construct)
        {
            Constructor = constructor;
        }

        public Expression Constructor { get; set; }
#if !MinimalReader
        public Construct(Expression constructor, ExpressionList arguments, SourceContext sctx)
            : base(arguments, NodeType.Construct)
        {
            Constructor = constructor;
            SourceContext = sctx;
        }

        public Construct(Expression constructor, ExpressionList arguments, TypeNode type)
            : base(arguments, NodeType.Construct)
        {
            Constructor = constructor;
            Type = type;
        }

        public Construct(Expression constructor, ExpressionList arguments, TypeNode type, SourceContext sctx)
            : base(arguments, NodeType.Construct)
        {
            Constructor = constructor;
            Type = type;
            SourceContext = sctx;
        }
#endif
    }

    public class ConstructArray : NaryExpression
    {
        public ConstructArray()
        {
            NodeType = NodeType.ConstructArray;
            Rank = 1;
        }

        public ConstructArray(TypeNode elementType, ExpressionList sizes, ExpressionList initializers)
            : base(sizes, NodeType.ConstructArray)
        {
            ElementType = elementType;
            Operands = sizes;
            Rank = sizes == null ? 1 : sizes.Count;
#if !MinimalReader
            Initializers = initializers;
#endif
        }

        public TypeNode ElementType { get; set; }

        public int Rank { get; set; }
#if !MinimalReader
        public TypeNode ElementTypeExpression;
        public ExpressionList Initializers;
        public Expression Owner;
#endif
#if !MinimalReader
        public ConstructArray(TypeNode elementType, ExpressionList initializers)
            : base(null, NodeType.ConstructArray)
        {
            ElementType = elementType;
            Initializers = initializers;
            Rank = 1;
            if (elementType != null)
                Type = elementType.GetArrayType(1);
        }

        public ConstructArray(TypeNode elementType, int rank, ExpressionList initializers)
            : base(null, NodeType.ConstructArray)
        {
            ElementType = elementType;
            Initializers = initializers;
            Rank = rank;
            if (elementType != null)
                Type = elementType.GetArrayType(1);
        }
#endif
    }
#if !MinimalReader
    public class ConstructFlexArray : NaryExpression
    {
        public TypeNode ElementType;
        public TypeNode ElementTypeExpression;
        public ExpressionList Initializers;

        public ConstructFlexArray()
        {
            NodeType = NodeType.ConstructFlexArray;
        }

        public ConstructFlexArray(TypeNode elementType, ExpressionList sizes, ExpressionList initializers)
            : base(sizes, NodeType.ConstructFlexArray)
        {
            ElementType = elementType;
            Operands = sizes;
            Initializers = initializers;
        }
    }

    public class ConstructDelegate : Expression
    {
        public TypeNode DelegateType;
        public TypeNode DelegateTypeExpression;
        public Identifier MethodName;
        public Expression TargetObject;

        public ConstructDelegate()
            : base(NodeType.ConstructDelegate)
        {
        }

        public ConstructDelegate(TypeNode delegateType, Expression targetObject, Identifier methodName)
            : base(NodeType.ConstructDelegate)
        {
            DelegateType = delegateType;
            MethodName = methodName;
            TargetObject = targetObject;
        }

        public ConstructDelegate(TypeNode delegateType, Expression targetObject, Identifier methodName,
            SourceContext sctx)
            : base(NodeType.ConstructDelegate)
        {
            DelegateType = delegateType;
            MethodName = methodName;
            TargetObject = targetObject;
            SourceContext = sctx;
        }
    }

    public class ConstructIterator : Expression
    {
        public Block Body;
        public TypeNode ElementType;
        public Class State;

        public ConstructIterator()
            : base(NodeType.ConstructIterator)
        {
        }

        public ConstructIterator(Class state, Block body, TypeNode elementType, TypeNode type)
            : base(NodeType.ConstructIterator)
        {
            State = state;
            Body = body;
            ElementType = elementType;
            Type = type;
        }
    }

    public class ConstructTuple : Expression
    {
        public FieldList Fields;

        public ConstructTuple()
            : base(NodeType.ConstructTuple)
        {
        }
    }

    public class CoerceTuple : ConstructTuple
    {
        public Expression OriginalTuple;
        public Local Temp;

        public CoerceTuple()
        {
            NodeType = NodeType.CoerceTuple;
        }
    }
#endif
    public class Indexer : NaryExpression
    {
        public Indexer()
        {
            NodeType = NodeType.Indexer;
        }

        public Indexer(Expression @object, ExpressionList arguments)
            : base(arguments, NodeType.Indexer)
        {
            Object = @object;
        }

        public Expression Object { get; set; }

        /// <summary>
        ///     This type is normally expected to be the same the value of Type. However, if the indexer applies to an array of
        ///     enums, then
        ///     Type will be the enum type and ElementType will be the underlying type of the enum.
        /// </summary>
        public TypeNode ElementType { get; set; }
#if !MinimalReader
        public Property CorrespondingDefaultIndexedProperty;
        public bool ArgumentListIsIncomplete;
#endif
#if !MinimalReader
        public Indexer(Expression Object, ExpressionList arguments, SourceContext sctx)
            : base(arguments, NodeType.Indexer)
        {
            this.Object = Object;
            SourceContext = sctx;
        }

        public Indexer(Expression Object, ExpressionList arguments, TypeNode elementType)
            : base(arguments, NodeType.Indexer)
        {
            this.Object = Object;
            ElementType = Type = elementType;
        }

        public Indexer(Expression Object, ExpressionList arguments, TypeNode elementType, SourceContext sctx)
            : base(arguments, NodeType.Indexer)
        {
            this.Object = Object;
            ElementType = Type = elementType;
            SourceContext = sctx;
        }
#endif
    }
#if !MinimalReader
    public class CollectionEnumerator : Expression
    {
        public Expression Collection;
        public Method DefaultIndexerGetter;
        public Expression ElementCoercion;
        public Local ElementLocal;
        public Method GetCurrent;
        public Method GetEnumerator;
        public Method LengthPropertyGetter;
        public Method MoveNext;

        public CollectionEnumerator()
            : base(NodeType.CollectionEnumerator)
        {
        }
    }

    /// <summary>
    ///     An expression that is used on the left hand as well as the right hand side of an assignment statement. For example,
    ///     e in (e += 1).
    /// </summary>
    public class LRExpression : Expression
    {
        public Expression Expression;
        public ExpressionList SubexpressionsToEvaluateOnce;
        public LocalList Temporaries;

        public LRExpression(Expression /*!*/ expression)
            : base(NodeType.LRExpression)
        {
            Expression = expression;
            Type = expression.Type;
        }
    }

    public class AssignmentExpression : Expression
    {
        public Statement AssignmentStatement;

        public AssignmentExpression()
            : base(NodeType.AssignmentExpression)
        {
        }

        public AssignmentExpression(AssignmentStatement assignment)
            : base(NodeType.AssignmentExpression)
        {
            AssignmentStatement = assignment;
        }
    }
#endif
#if !MinimalReader || FxCop
    public class BlockExpression : Expression
    {
        public Block Block;

        public BlockExpression()
            : base(NodeType.BlockExpression)
        {
        }

        public BlockExpression(Block block)
            : base(NodeType.BlockExpression)
        {
            Block = block;
        }

        public BlockExpression(Block block, TypeNode type)
            : base(NodeType.BlockExpression)
        {
            Block = block;
            Type = type;
        }

        public BlockExpression(Block block, TypeNode type, SourceContext sctx)
            : base(NodeType.BlockExpression)
        {
            Block = block;
            Type = type;
            SourceContext = sctx;
        }
    }
#endif
#if !MinimalReader
    public class AnonymousNestedFunction : Expression
    {
        public Block Body;
        public Expression Invocation;
        public Method Method;
        public ParameterList Parameters;

        public AnonymousNestedFunction()
            : base(NodeType.AnonymousNestedFunction)
        {
        }

        public AnonymousNestedFunction(ParameterList parameters, Block body)
            : base(NodeType.AnonymousNestedFunction)
        {
            Parameters = parameters;
            Body = body;
        }

        public AnonymousNestedFunction(ParameterList parameters, Block body, SourceContext sctx)
            : base(NodeType.AnonymousNestedFunction)
        {
            Parameters = parameters;
            Body = body;
            SourceContext = sctx;
        }
    }
#endif
    public class Instruction : Node
    {
        public Instruction()
            : base(NodeType.Instruction)
        {
        }

        public Instruction(OpCode opCode, int offset)
            : this(opCode, offset, null)
        {
        }

        public Instruction(OpCode opCode, int offset, object value)
            : base(NodeType.Instruction)
        {
            OpCode = opCode;
            Offset = offset;
            Value = value;
        }

        /// <summary>The actual value of the opcode</summary>
        public OpCode OpCode { get; set; }

        /// <summary>The offset from the start of the instruction stream of a method</summary>
        public int Offset { get; set; }

        /// <summary>Immediate data such as a string, the address of a branch target, or a metadata reference, such as a Field</summary>
        public object Value { get; set; }
    }

    public class Statement : Node
    {
#if FxCop
    internal int ILOffset;
#endif
#if !FxCop && ILOFFSETS
        public int ILOffset;
#endif
        public Statement(NodeType nodeType)
            : base(nodeType)
        {
        }
#if !MinimalReader
        public Statement(NodeType nodeType, SourceContext sctx)
            : base(nodeType)
        {
            SourceContext = sctx;
        }
#endif
    }

    public class Block : Statement
    {
#if !MinimalReader
        public bool Checked;
        public bool SuppressCheck;
#endif
#if !MinimalReader || !NoWriter
        public bool HasLocals;
#endif
#if !MinimalReader && !CodeContracts
    public bool IsUnsafe;
    public BlockScope Scope;
#endif
        public Block()
            : base(NodeType.Block)
        {
        }

        public Block(StatementList statements)
            : base(NodeType.Block)
        {
#if CLOUSOT || CodeContracts
            Contract.Ensures(Statements == statements);
#endif
            Statements = statements;
        }
#if !MinimalReader && !CodeContracts
    public Block(StatementList statements, SourceContext sourceContext)
      : base(NodeType.Block){
      this.SourceContext = sourceContext;
      this.statements = statements;
    }
    public Block(StatementList statements, bool Checked, bool SuppressCheck, bool IsUnsafe)
      : base(NodeType.Block){
      this.Checked = Checked;
      this.IsUnsafe = IsUnsafe;
      this.SuppressCheck = SuppressCheck;
      this.statements = statements;
    }
    public Block(StatementList statements, SourceContext sourceContext, bool Checked, bool SuppressCheck, bool IsUnsafe)
      : base(NodeType.Block){
      this.Checked = Checked;
      this.IsUnsafe = IsUnsafe;
      this.SuppressCheck = SuppressCheck;
      this.SourceContext = sourceContext;
      this.statements = statements;
    }
    public override string ToString() {
      return "B#" + this.UniqueKey.ToString();
    }
#endif
        public StatementList Statements { get; set; }
    }
#if !MinimalReader
    public class LabeledStatement : Block
    {
        public Identifier Label;
        public Statement Statement;

        public LabeledStatement()
        {
            NodeType = NodeType.LabeledStatement;
        }
    }

    public class FunctionDeclaration : Statement
    {
        public Block Body;
        public Method Method;
        public Identifier Name;
        public ParameterList Parameters;
        public TypeNode ReturnType;
        public TypeNode ReturnTypeExpression;

        public FunctionDeclaration()
            : base(NodeType.FunctionDeclaration)
        {
        }

        public FunctionDeclaration(Identifier name, ParameterList parameters, TypeNode returnType, Block body)
            : base(NodeType.FunctionDeclaration)
        {
            Name = name;
            Parameters = parameters;
            ReturnType = returnType;
            Body = body;
        }
    }

    public class Assertion : Statement
    {
        public Expression Condition;

        // a string that a user wants associated with the assertion
        public Literal userMessage;

        public Assertion()
            : base(NodeType.Assertion)
        {
        }

        public Assertion(Expression condition)
            : base(NodeType.Assertion)
        {
            Condition = condition;
        }
    }

    public class Assumption : Statement
    {
        public Expression Condition;

        // a string that a user wants associated with the assumption
        public Literal userMessage;

        public Assumption()
            : base(NodeType.Assumption)
        {
        }

        public Assumption(Expression condition)
            : base(NodeType.Assumption)
        {
            Condition = condition;
        }
    }
#endif
    public class AssignmentStatement : Statement
    {
        public AssignmentStatement()
            : base(NodeType.AssignmentStatement)
        {
            Operator = NodeType.Nop;
        }

        public AssignmentStatement(Expression target, Expression source)
            : this(target, source, NodeType.Nop)
        {
        }
#if !MinimalReader
        public AssignmentStatement(Expression target, Expression source, SourceContext context)
            : this(target, source, NodeType.Nop)
        {
            SourceContext = context;
        }
#endif
        public AssignmentStatement(Expression target, Expression source, NodeType @operator)
            : base(NodeType.AssignmentStatement)
        {
            Target = target;
            Source = source;
            Operator = @operator;
        }
#if !MinimalReader
        public AssignmentStatement(Expression target, Expression source, NodeType Operator, SourceContext context)
            : this(target, source, Operator)
        {
            SourceContext = context;
        }
#endif
        public NodeType Operator { get; set; }

        public Expression Source { get; set; }

        public Expression Target { get; set; }
#if !MinimalReader
        public Method OperatorOverload;
        ///<summary>A Type two which both operands must be coerced before carrying out the operation (if any).</summary>
        public TypeNode UnifiedType;
#endif
    }

    public class ExpressionStatement : Statement
    {
        public ExpressionStatement()
            : base(NodeType.ExpressionStatement)
        {
        }

        public ExpressionStatement(Expression expression)
            : base(NodeType.ExpressionStatement)
        {
            Expression = expression;
        }
#if !MinimalReader
        public ExpressionStatement(Expression expression, SourceContext sctx)
            : base(NodeType.ExpressionStatement)
        {
            Expression = expression;
            SourceContext = sctx;
        }
#endif
        public Expression Expression { get; set; }
    }

    public class Branch : Statement
    {
        public bool BranchIfUnordered;
        internal bool shortOffset;

        public Branch()
            : base(NodeType.Branch)
        {
        }

        public Branch(Expression condition, Block target, bool shortOffset, bool unordered, bool leavesExceptionBlock)
            : base(NodeType.Branch)
        {
            BranchIfUnordered = unordered;
            Condition = condition;
            LeavesExceptionBlock = leavesExceptionBlock;
            this.shortOffset = shortOffset;
            Target = target;
        }

        public Expression Condition { get; set; }

        public bool LeavesExceptionBlock { get; set; }

        public bool ShortOffset
        {
            get { return shortOffset; }
            set { shortOffset = value; }
        }

        public Block Target { get; set; }
#if !MinimalReader
        public Branch(Expression condition, Block target)
            : this(condition, target, false, false, false)
        {
        }

        public Branch(Expression condition, Block target, SourceContext sourceContext)
            : this(condition, target, false, false, false)
        {
            SourceContext = sourceContext;
        }

        public Branch(Expression condition, Block target, SourceContext sourceContext, bool unordered)
            : this(condition, target, false, false, false)
        {
            BranchIfUnordered = unordered;
            SourceContext = sourceContext;
        }
#endif
    }
#if FxCop
  public class ReturnNode : ExpressionStatement{
    public ReturnNode()
#else
    public class Return : ExpressionStatement
    {
        public Return()
#endif
        {
            NodeType = NodeType.Return;
        }
#if FxCop
    public ReturnNode(Expression expression)
#else
        public Return(Expression expression)
#endif
            : base(expression)
        {
            NodeType = NodeType.Return;
        }
#if !MinimalReader
        public Return(SourceContext sctx)
        {
            NodeType = NodeType.Return;
            SourceContext = sctx;
        }

        public Return(Expression expression, SourceContext sctx)
            : base(expression)
        {
            NodeType = NodeType.Return;
            SourceContext = sctx;
        }
#endif
    }
#if !MinimalReader
    /// <summary>
    ///     Represents the return value in a post condition
    /// </summary>
    public class ReturnValue : Expression
    {
        public ReturnValue() : base(NodeType.ReturnValue)
        {
        }

        public ReturnValue(SourceContext sc)
            : this()
        {
            SourceContext = sc;
        }

        public ReturnValue(TypeNode returnType, SourceContext sc)
            : this(sc)
        {
            Type = returnType;
        }

        public ReturnValue(TypeNode returnType)
            : this()
        {
            Type = returnType;
        }
    }

#if !CodeContracts
  public class Yield : ExpressionStatement{
    public Yield()
      : base(){
      this.NodeType = NodeType.Yield;
    }
    public Yield(Expression expression)
      : base(expression){
      this.NodeType = NodeType.Yield;
    }
    public Yield(Expression expression, SourceContext sctx)
      : base(expression){
      this.NodeType = NodeType.Yield;
      this.SourceContext = sctx;
    }
  }
  public class Try : Statement{
    private CatchList catchers;
    private FilterList filters;
    private FaultHandlerList faultHandlers;
    private Finally finallyClause;
    private Block tryBlock;
    public Try()
      : base(NodeType.Try){
    }
    public Try(Block tryBlock, CatchList catchers, FilterList filters, FaultHandlerList faultHandlers, Finally Finally)
      : base(NodeType.Try){
      this.catchers = catchers;
      this.faultHandlers = faultHandlers;
      this.filters = filters;
      this.finallyClause = Finally;
      this.tryBlock = tryBlock;
    }
    public CatchList Catchers{
      get{return this.catchers;}
      set{this.catchers = value;}
    }
    public FilterList Filters{
      get{return this.filters;}
      set{this.filters = value;}
    }
    public FaultHandlerList FaultHandlers{
      get{return this.faultHandlers;}
      set{this.faultHandlers = value;}
    }
    public Finally Finally{
      get{return this.finallyClause;}
      set{this.finallyClause = value;}
    }
    public Block TryBlock{
      get{return this.tryBlock;}
      set{this.tryBlock = value;}
    }
  }
  public class Catch : Statement{
    private Block block;
    private TypeNode type;
    private Expression variable;
    public TypeNode TypeExpression;
    public Catch()
      : base(NodeType.Catch){
    }
    public Catch(Block block, Expression variable, TypeNode type)
      : base(NodeType.Catch){
      this.block = block;
      this.variable = variable;
      this.type = type;
    }
    public Block Block{
      get{return this.block;}
      set{this.block = value;}
    }
    public TypeNode Type{
      get{return this.type;}
      set{this.type = value;}
    }
    public Expression Variable{
      get{return this.variable;}
      set{this.variable = value;}
    }
  }
  public class Finally : Statement{
    private Block block;
    public Finally()
      : base(NodeType.Finally){
    }
    public Finally(Block block)
      : base(NodeType.Finally){
      this.block = block;
    }
    public Block Block{
      get{return this.block;}
      set{this.block = value;}
    }
  }
#endif
#endif
    public class EndFinally : Statement
    {
        public EndFinally()
            : base(NodeType.EndFinally)
        {
        }
    }
#if !MinimalReader || FxCop
    public class Filter : Statement
    {
#if FxCop
    internal int handlerEnd;
#endif
        public Filter()
            : base(NodeType.Filter)
        {
        }

        public Filter(Block block, Expression expression)
            : base(NodeType.Filter)
        {
            Block = block;
            Expression = expression;
        }

        public Block Block { get; set; }

        public Expression Expression { get; set; }
    }
#endif
    public class EndFilter : Statement
    {
        public EndFilter()
            : base(NodeType.EndFilter)
        {
        }

        public EndFilter(Expression value)
            : base(NodeType.EndFilter)
        {
            Value = value;
        }

        public Expression Value { get; set; }
    }
#if !MinimalReader || FxCop
    public class FaultHandler : Statement
    {
#if FxCop
    internal int handlerEnd;
#endif
        public FaultHandler()
            : base(NodeType.FaultHandler)
        {
        }

        public FaultHandler(Block block)
            : base(NodeType.FaultHandler)
        {
            Block = block;
        }

        public Block Block { get; set; }
    }
#endif
#if FxCop
  public class ThrowNode : Statement{
#else
    public class Throw : Statement
    {
#endif
#if FxCop
    public ThrowNode()
      : base(NodeType.Throw){
    }
    public ThrowNode(Expression expression)
      : base(NodeType.Throw){
      this.expression = expression;
    }
#else
        public Throw()
            : base(NodeType.Throw)
        {
        }

        public Throw(Expression expression)
            : base(NodeType.Throw)
        {
            Expression = expression;
        }
#endif
#if !MinimalReader
        public Throw(Expression expression, SourceContext context)
            : base(NodeType.Throw)
        {
            Expression = expression;
            SourceContext = context;
        }
#endif
        public Expression Expression { get; set; }
    }
#if !MinimalReader && !CodeContracts
  public class If : Statement{
    public Expression Condition;
    public Block TrueBlock;
    public Block FalseBlock;
    public SourceContext ConditionContext;
    public SourceContext ElseContext;
    public SourceContext EndIfContext;
    public If()
      : base(NodeType.If){
    }
    public If(Expression condition, Block trueBlock, Block falseBlock)
      : base(NodeType.If){
      this.Condition = condition;
      if (condition != null)
        this.ConditionContext = condition.SourceContext;
      this.TrueBlock = trueBlock;
      this.FalseBlock = falseBlock;
    }
  }
  public class For : Statement{
    public Block Body;
    public Expression Condition;
    public StatementList Incrementer;
    public StatementList Initializer;
    public ExpressionList Invariants;
    public For()
      : base(NodeType.For){
    }
    public For(StatementList initializer, Expression condition, StatementList incrementer, Block body)
      : base(NodeType.For){
      this.Body = body;
      this.Condition = condition;
      this.Incrementer = incrementer;
      this.Initializer = initializer;
    }
  }
  public class ForEach : Statement{
    public Block Body;
    public Expression SourceEnumerable;
    public BlockScope ScopeForTemporaryVariables;
    public Expression TargetVariable;
    public TypeNode TargetVariableType;
    public TypeNode TargetVariableTypeExpression;
    public Expression InductionVariable;
    public ExpressionList Invariants;
    public bool StatementTerminatesNormallyIfEnumerableIsNull = true;
    public bool StatementTerminatesNormallyIfEnumeratorIsNull = true;
    public ForEach()
      : base(NodeType.ForEach){
    }
    public ForEach(TypeNode targetVariableType, Expression targetVariable, Expression sourceEnumerable, Block body)
      : base(NodeType.ForEach){
      this.TargetVariable = targetVariable;
      this.TargetVariableType = targetVariableType;
      this.SourceEnumerable = sourceEnumerable;
      this.Body = body;
    }
  }
  public class Exit : Statement{
    public Literal Level;
    public Exit()
      : base(NodeType.Exit){
    }
    public Exit(Literal level)
      : base(NodeType.Exit){
      this.Level = level;
    }
  }
  public class Continue : Statement{
    public Literal Level;
    public Continue()
      : base(NodeType.Continue){
    }
    public Continue(Literal level)
      : base(NodeType.Continue){
      this.Level = level;
    }
  }
  public class Switch : Statement{
    public SwitchCaseList Cases;
    public Expression Expression;
    public Local Nullable;
    public Expression NullableExpression;
    public BlockScope Scope;
    public Switch()
      : base(NodeType.Switch){
    }
    public Switch(Expression expression, SwitchCaseList cases)
      : base(NodeType.Switch){
      this.Cases = cases;
      this.Expression = expression;
    }
  }
  public class SwitchCase : Node{
    public Block Body;
    public Expression Label;
    public SwitchCase()
      : base(NodeType.SwitchCase){
    }
    public SwitchCase(Expression label, Block body)
      : base(NodeType.SwitchCase){
      this.Body = body;
      this.Label = label;
    }
  }
  public class GotoCase : Statement{
    public Expression CaseLabel;
    public GotoCase(Expression caseLabel)
      : base(NodeType.GotoCase){
      this.CaseLabel = caseLabel;
    }
  }
#endif
    public class SwitchInstruction : Statement
    {
        public SwitchInstruction()
            : base(NodeType.SwitchInstruction)
        {
        }

        public SwitchInstruction(Expression expression, BlockList targets)
            : base(NodeType.SwitchInstruction)
        {
            Expression = expression;
            Targets = targets;
        }

        public Expression Expression { get; set; }

        public BlockList Targets { get; set; }
    }
#if !MinimalReader && !CodeContracts
  public class Typeswitch : Statement{
    public TypeswitchCaseList Cases;
    public Expression Expression;
    public Typeswitch()
      : base(NodeType.Typeswitch){
    }
    public Typeswitch(Expression expression, TypeswitchCaseList cases)
      : base(NodeType.Typeswitch){
      this.Cases = cases;
      this.Expression = expression;
    }
  }
  public class TypeswitchCase : Node{
    public Block Body;
    public TypeNode LabelType;
    public TypeNode LabelTypeExpression;
    public Expression LabelVariable;
    public TypeswitchCase()
      : base(NodeType.TypeswitchCase){
    }
    public TypeswitchCase(TypeNode labelType, Expression labelVariable, Block body)
      : base(NodeType.TypeswitchCase){
      this.Body = body;
      this.LabelType = labelType;
      this.LabelVariable = labelVariable;
    }
  }
  public class While : Statement{
    public Expression Condition;
    public ExpressionList Invariants;
    public Block Body;
    public While()
      : base(NodeType.While){
    }
    public While(Expression condition, Block body)
      : base(NodeType.While){
      this.Condition = condition;
      this.Body = body;
    }
  }
  public class DoWhile : Statement{
    public Expression Condition;
    public ExpressionList Invariants;
    public Block Body;
    public DoWhile()
      : base(NodeType.DoWhile){
    }
    public DoWhile(Expression condition, Block body)
      : base(NodeType.DoWhile){
      this.Condition = condition;
      this.Body = body;
    }
  }
  public class Repeat : Statement{
    public Expression Condition;
    public Block Body;
    public Repeat()
      : base(NodeType.Repeat){
    }
    public Repeat(Expression condition, Block body)
      : base(NodeType.Repeat){
      this.Condition = condition;
      this.Body = body;
    }
  }
  public class Fixed : Statement{
    public Statement Declarators;
    public Block Body;
    public BlockScope ScopeForTemporaryVariables;
    public Fixed()
      : base(NodeType.Fixed){
    }
  }
  public class Lock : Statement{
    public Expression Guard;
    public Block Body;
    public BlockScope ScopeForTemporaryVariable;
    public Lock()
      : base(NodeType.Lock){
    }
  }
  public class ResourceUse : Statement{
    public Statement ResourceAcquisition;
    public Block Body;
    public BlockScope ScopeForTemporaryVariable;
    public ResourceUse()
      : base(NodeType.ResourceUse){
    }
  }
  public class Goto : Statement{
    public Identifier TargetLabel;
    public Goto()
      : base(NodeType.Goto){
    }
    public Goto(Identifier targetLabel)
      : base(NodeType.Goto){
      this.TargetLabel = targetLabel;
    }
  }
  public class VariableDeclaration : Statement{
    public Expression Initializer;
    public Identifier Name;
    public TypeNode Type;
    public TypeNode TypeExpression;
    public Field Field;
    public VariableDeclaration()
      : base(NodeType.VariableDeclaration){
    }
    public VariableDeclaration(Identifier name, TypeNode type, Expression initializer)
      : base(NodeType.VariableDeclaration){
      this.Initializer = initializer;
      this.Name = name;
      this.Type = type;
    }
  }
  public class LocalDeclaration : Node{
    public Field Field; 
    public Identifier Name;
    public Expression InitialValue;
    /// <summary>
    /// Used when converting a declaration with initializer into an assignment statement.
    /// Usually Nop, but could be set to CopyReference to avoid dereferencing on either side.
    /// </summary>
    public NodeType AssignmentNodeType = NodeType.Nop;
    public LocalDeclaration()
      : base(NodeType.LocalDeclaration){
    }
    public LocalDeclaration(Identifier name, Expression initialValue)
      : base(NodeType.LocalDeclaration){
      this.Name = name;
      this.InitialValue = initialValue;
    }
    public LocalDeclaration(Identifier name, Expression initialValue, NodeType assignmentNodeType)
      : base(NodeType.LocalDeclaration){
      this.Name = name;
      this.InitialValue = initialValue;
      this.AssignmentNodeType = assignmentNodeType;
    }

  }
  public class LocalDeclarationsStatement : Statement{
    public bool Constant;
    public bool InitOnly;
    public TypeNode Type;
    public TypeNode TypeExpression;
    public LocalDeclarationList Declarations;
    public LocalDeclarationsStatement()
      : base(NodeType.LocalDeclarationsStatement){
    }
    public LocalDeclarationsStatement(LocalDeclaration ldecl, TypeNode type)
      : base(NodeType.LocalDeclarationsStatement){
      Declarations = new LocalDeclarationList();
      Declarations.Add(ldecl);
      this.Type = type;
    }
  }
  public class StatementSnippet : Statement{
    public IParserFactory ParserFactory;

    public StatementSnippet()
      : base(NodeType.StatementSnippet){
    }
    public StatementSnippet(IParserFactory parserFactory, SourceContext sctx)
      : base(NodeType.StatementSnippet){
      this.ParserFactory = parserFactory;
      this.SourceContext = sctx;
    }
  }
  /// <summary>
  /// Associates an identifier with a type or a namespace or a Uri or a list of assemblies. 
  /// In C# alias identifiers are used as root identifiers in qualified expressions, or as identifier prefixes.
  /// </summary>
  public class AliasDefinition : Node{
    
    /// <summary>The identifier that serves as an alias for the type, namespace, Uri or list of assemblies.</summary>
    public Identifier Alias;

    /// <summary>The list of assemblies being aliased.</summary>
    public AssemblyReferenceList AliasedAssemblies;
    
    /// <summary>The expression that was (or should be) resolved into a type, namespace or Uri.</summary>
    public Expression AliasedExpression;
    
    /// <summary>The namespace being aliased.</summary>
    public Identifier AliasedNamespace;
    
    /// <summary>A reference to the type being aliased.</summary>
    public TypeReference AliasedType;
    
    /// <summary>The Uri being aliased.</summary>
    public Identifier AliasedUri;

    /// <summary>
    /// If an alias definition conflicts with a type definition and this causes an ambiguity, the conflicting type is stored here
    /// by the code that detects the ambiguity. A later visitor is expected to report an error if this is not null.
    /// </summary>
    public TypeNode ConflictingType;

    public bool RestrictToInterfaces;
    public bool RestrictToClassesAndInterfaces;

    public AliasDefinition()
      : base(NodeType.AliasDefinition){
    }
    public AliasDefinition(Identifier alias, Expression aliasedExpression)
      : base(NodeType.AliasDefinition){
      this.Alias = alias;
      this.AliasedExpression = aliasedExpression;
    }
    public AliasDefinition(Identifier alias, Expression aliasedExpression, SourceContext sctx)
      : base(NodeType.AliasDefinition){
      this.Alias = alias;
      this.AliasedExpression = aliasedExpression;
      this.SourceContext = sctx;
    }
  }
  public class UsedNamespace : Node{
    public Identifier Namespace;
    public Identifier URI;
    public UsedNamespace()
      : base(NodeType.UsedNamespace){
    }
    public UsedNamespace(Identifier Namespace)
      : base(NodeType.UsedNamespace){
      this.Namespace = Namespace;
    }
    public UsedNamespace(Identifier Namespace, SourceContext sctx)
      : base(NodeType.UsedNamespace){
      this.Namespace = Namespace;
      this.SourceContext = sctx;
    }
  }
#endif
#if !FxCop
    public class ExceptionHandler : Node
    {
        public ExceptionHandler()
            : base(NodeType.ExceptionHandler)
        {
        }

        public NodeType HandlerType { get; set; }

        public Block TryStartBlock { get; set; }

        public Block BlockAfterTryEnd { get; set; }

        public Block HandlerStartBlock { get; set; }

        public Block BlockAfterHandlerEnd { get; set; }

        public Block FilterExpression { get; set; }

        public TypeNode FilterType { get; set; }
    }
#endif
    public class AttributeNode : Node
    {
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly AttributeNode DoesNotExist = new AttributeNode();

        private bool allowMultiple;
        private bool inherited;
#if !MinimalReader
        public bool IsPseudoAttribute;
#endif
        private TypeNode type;
        private AttributeNode usageAttribute;
        private AttributeTargets validOn;

        public AttributeNode()
            : base(NodeType.Attribute)
        {
        }

        public AttributeNode(Expression constructor, ExpressionList expressions)
            : base(NodeType.Attribute)
        {
            Constructor = constructor;
            Expressions = expressions;
            Target = AttributeTargets.All;
        }
#if !MinimalReader
        public AttributeNode(Expression constructor, ExpressionList expressions, AttributeTargets target)
            : base(NodeType.Attribute)
        {
            Constructor = constructor;
            Expressions = expressions;
            Target = target;
        }
#endif
        public Expression Constructor { get; set; }

        /// <summary>
        ///     Invariant: positional arguments occur first and in order in the expression list. Named arguments
        ///     follow posititional arguments in any order.
        /// </summary>
        public ExpressionList Expressions { get; set; }

        public AttributeTargets Target { get; set; }

        public virtual bool AllowMultiple
        {
            get
            {
                if (usageAttribute == null) GetUsageInformation();
                return allowMultiple;
            }
            set { allowMultiple = value; }
        }

        public virtual bool Inherited
        {
            get
            {
                if (usageAttribute == null) GetUsageInformation();
                return inherited;
            }
            set { inherited = value; }
        }

        public virtual AttributeTargets ValidOn
        {
            get
            {
                if (usageAttribute == null) GetUsageInformation();
                return validOn;
            }
            set { validOn = value; }
        }

        public virtual TypeNode Type
        {
            get
            {
                if (type == null)
                {
                    var mb = Constructor as MemberBinding;
                    var cons = mb == null ? null : mb.BoundMember;
                    type = cons == null ? null : cons.DeclaringType;
                }

                return type;
            }
            set { type = value; }
        }

        private void GetUsageInformation()
        {
            AttributeNode attr = null;
            var attrType = Type;
            while (attrType != null)
            {
                attr = attrType.GetAttribute(SystemTypes.AttributeUsageAttribute);
                if (attr != null) break;
                attrType = attrType.BaseType;
            }

            if (attr == null)
            {
                usageAttribute = DoesNotExist;
                return;
            }

            var args = attr.Expressions;
            if (args == null || args.Count < 1) return;
            var lit = args[0] as Literal;
            if (lit == null || !(lit.Value is int))
            {
#if ExtendedRuntime
        MemberBinding mb = args[0] as MemberBinding;
        if (mb != null) {
          Field f = mb.BoundMember as Field;
          if (f != null && f.IsLiteral) {
            lit = f.Initializer as Literal;
          }
        }
        if (lit == null || !(lit.Value is int))
#endif
                return;
            }

            //^ assert lit.Value != null;
            validOn = (AttributeTargets)(int)lit.Value;
            for (int i = 1, n = args.Count; i < n; i++)
            {
                var narg = args[i] as NamedArgument;
                if (narg == null || narg.Name == null) continue;
                lit = narg.Value as Literal;
                if (lit == null) continue;
                if (narg.Name.UniqueIdKey == StandardIds.AllowMultiple.UniqueIdKey)
                {
                    if (lit.Value == null || !(lit.Value is bool)) continue;
                    allowMultiple = (bool)lit.Value;
                }
                else if (narg.Name.UniqueIdKey == StandardIds.Inherited.UniqueIdKey)
                {
                    if (lit.Value == null || !(lit.Value is bool)) continue;
                    inherited = (bool)lit.Value;
                }
            }
#if !MinimalReader
            if (!allowMultiple)
            {
                var n = attrType.Attributes.Count;
                for (var i = 0; i < n; i++)
                {
                    var a = attrType.Attributes[i];
                    if (a != null && a.Type != null && a.Type.Name != null &&
                        a.Type.Name.UniqueIdKey == StandardIds.AllowMultipleAttribute.UniqueIdKey)
                        if (a.Type.Namespace != null &&
                            a.Type.Namespace.UniqueIdKey == StandardIds.WindowsFoundation.UniqueIdKey)
                        {
                            allowMultiple = true;
                            break;
                        }
                }
            }
#endif
        }

        public Expression GetPositionalArgument(int position)
        {
            if (Expressions == null || Expressions.Count <= position) return null;
            var e = Expressions[position];
            var na = e as NamedArgument;
            if (na != null) return null;
            return e;
        }

        public Expression GetNamedArgument(Identifier name)
        {
            if (name == null || Expressions == null) return null;
            foreach (var e in Expressions)
            {
                var na = e as NamedArgument;
                if (na == null) continue;
                if (na.Name == null) continue;
                if (na.Name.UniqueIdKey == name.UniqueIdKey) return na.Value;
            }

            return null;
        }
#if !NoReflection
        public virtual Attribute GetRuntimeAttribute()
        {
            var mb = Constructor as MemberBinding;
            if (mb == null) return null;
            var constr = mb.BoundMember as InstanceInitializer;
            if (constr == null) return null;
            var parameters = constr.Parameters;
            var paramCount = parameters == null ? 0 : parameters.Count;
            var argumentValues = new object[paramCount];
            var argumentExpressions = Expressions;
            var exprCount = argumentExpressions == null ? 0 : argumentExpressions.Count;
            for (int i = 0, j = 0; i < paramCount; i++)
            {
                if (j >= exprCount) return null;
                //^ assert argumentExpressions != null;
                var argExpr = argumentExpressions[j++];
                var lit = argExpr as Literal;
                if (lit == null) continue;
                argumentValues[i] = GetCoercedLiteralValue(lit.Type, lit.Value);
            }

            var attr = ConstructAttribute(constr, argumentValues);
            if (attr == null) return null;
            for (var i = 0; i < exprCount; i++)
            {
                //^ assert argumentExpressions != null;
                var namedArg = argumentExpressions[i] as NamedArgument;
                if (namedArg == null) continue;
                if (namedArg.Name == null) continue;
                var lit = namedArg.Value as Literal;
                if (lit == null) continue;
                var val = GetCoercedLiteralValue(lit.Type, lit.Value);
                if (namedArg.IsCustomAttributeProperty)
                {
                    var t = constr.DeclaringType;
                    while (t != null)
                    {
                        var prop = t.GetProperty(namedArg.Name);
                        if (prop != null)
                        {
                            SetAttributeProperty(prop, attr, val);
                            t = null;
                        }
                        else
                        {
                            t = t.BaseType;
                        }
                    }
                }
                else
                {
                    var t = constr.DeclaringType;
                    while (t != null)
                    {
                        var f = constr.DeclaringType.GetField(namedArg.Name);
                        if (f != null)
                        {
                            var fieldInfo = f.GetFieldInfo();
                            if (fieldInfo != null) fieldInfo.SetValue(attr, val);
                            t = null;
                        }
                        else
                        {
                            t = t.BaseType;
                        }
                    }
                }
            }

            return attr;
        }

        /// <summary>
        ///     Gets the value of the literal coercing literals of TypeNode, EnumNode, TypeNode[], and EnumNode[] as needed.
        /// </summary>
        /// <param name="type">A TypeNode representing the type of the literal</param>
        /// <param name="value">The value of the literal</param>
        /// <returns>An object that has been coerced to the appropiate runtime type</returns>
        protected object GetCoercedLiteralValue(TypeNode type, object value)
        {
            if (type == null || value == null) return null;
            switch (type.typeCode)
            {
                case ElementType.Class:
                    return ((TypeNode)value).GetRuntimeType();
                case ElementType.ValueType:
                    return Enum.ToObject(type.GetRuntimeType(), value);
                case ElementType.SzArray:
                    return GetCoercedArrayLiteral((ArrayType)type, (Array)value);
                default:
                    var lit = value as Literal;
                    if (lit != null && type == CoreSystemTypes.Object && lit.Type is EnumNode)
                        return GetCoercedLiteralValue(lit.Type, lit.Value);
                    break;
            }

            return value;
        }

        /// <summary>
        ///     Gets the array literal in arrayValue coercing TypeNode[] and EnumNode[] as needed.
        /// </summary>
        /// <param name="arrayType">A TypeNode representing the array type</param>
        /// <param name="arrayValue">The value of the array literal to coerce</param>
        /// <returns>An Array object that has been coerced to the appropriate runtime type</returns>
        protected Array GetCoercedArrayLiteral(ArrayType arrayType, Array arrayValue)
        {
            if (arrayType == null) return null;
            if (arrayValue == null) return null;
            // Multi-dimensional arrays are not legal in attribute instances according section 17.1.3 of the C# 1.0 spec
            if (arrayValue.Rank != 1) return null;
            var elemType = arrayType.ElementType;
            if (elemType.typeCode != ElementType.ValueType && elemType.typeCode != ElementType.Class)
                return arrayValue;
            var arraySize = arrayValue.GetLength(0);
            var et = elemType.GetRuntimeType();
            if (et == null) return null;
            var val = Array.CreateInstance(et, arraySize);
            for (var i = 0; i < arraySize; i++)
                val.SetValue(GetCoercedLiteralValue(elemType, arrayValue.GetValue(i)), i);
            return val;
        }

        private static void SetAttributeProperty(Property /*!*/ prop, Attribute attr, object val)
        {
            //This could execute partially trusted code, so set up a very restrictive execution environment
            //TODO: skip this if the attribute is from a trusted assembly
            var propInfo = prop.GetPropertyInfo();
            if (propInfo == null) return;
            //Because we invoke the setter through reflection, a stack walk is performed. The following two commented-out statements
            //would cause the stack walk to fail.
            //For now, we will run the setter in full trust until we work around this.
            //For VS2005 and later, we will construct a DynamicMethod, wrap it in a delegate, and invoke that.

            //System.Security.PermissionSet perm = new System.Security.PermissionSet(System.Security.Permissions.PermissionState.None);
            //perm.PermitOnly();
            try
            {
                propInfo.SetValue(attr, val, null);
            }
            catch
            {
            }
        }

        private static Attribute ConstructAttribute(InstanceInitializer /*!*/ constr, object[] argumentValues)
        {
            //This could execute partially trusted code, so set up a very restrictive execution environment
            //TODO: skip this if the attribute is from a trusted assembly
            var consInfo = constr.GetConstructorInfo();
            if (consInfo == null) return null;
            //Because we invoke the constructor through reflection, a stack walk is performed. The following two commented-out statements
            //would cause the stack walk to fail.
            //For VS2003 and earlier, we will run the constructor in full trust.
            //For VS2005 and later, we will construct a DynamicMethod, wrap it in a delegate, and invoke that.

            //System.Security.PermissionSet perm = new System.Security.PermissionSet(System.Security.Permissions.PermissionState.None);
            //perm.PermitOnly();
            try
            {
                return consInfo.Invoke(argumentValues) as Attribute;
            }
            catch
            {
            }

            return null;
        }
#endif
    }

    public class SecurityAttribute : Node
    {
        protected string serializedPermissions;

        public SecurityAttribute()
            : base(NodeType.SecurityAttribute)
        {
        }

        public SecurityAction Action { get; set; }

        public AttributeList PermissionAttributes { get; set; }

        public string SerializedPermissions
        {
            get
            {
#if !NoReflection || FxCop
                if (serializedPermissions == null && PermissionAttributes != null)
                    lock (this)
                    {
                        if (serializedPermissions != null) return serializedPermissions;
                        var permissions = Permissions;
                        if (permissions == null) return null;
                        var xml = permissions.ToXml();
                        if (xml == null) return null;
                        serializedPermissions = xml.ToString();
                        //TODO: if the target platform is different from the host platform, replace references to host platform
                        //assemblies with references to target platform assemblies
                    }
#endif
                return serializedPermissions;
            }
            set { serializedPermissions = value; }
        }
#if !NoReflection || FxCop
        protected PermissionSet permissions;
        public PermissionSet Permissions
        {
            get
            {
                if (this.permissions == null)
                    lock (this)
                    {
                        if (this.permissions != null) return this.permissions;
                        PermissionSet permissions = null;
#if !FxCop
                        if (PermissionAttributes != null)
                        {
                            permissions = InstantiatePermissionAttributes();
                        }
                        else if (serializedPermissions != null)
                        {
                            permissions = new PermissionSet(PermissionState.None);
                            permissions.FromXml(GetSecurityElement());
                        }
#elif !TestBuild
            permissions = PermissionsHelper.GetPermissions(this);
#endif
                        this.permissions = permissions;
                    }

                return this.permissions;
            }
            set { permissions = value; }
        }
#endif
#if !NoReflection
        protected SecurityElement GetSecurityElement()
        {
#if WHIDBEY
            return SecurityElement.FromString(serializedPermissions);
#else
      System.Reflection.Assembly mscorlib = CoreSystemTypes.SystemAssembly.GetRuntimeAssembly();
      if (mscorlib == null) { Debug.Fail(""); return null; }
      Type parserType = mscorlib.GetType("System.Security.Util.Parser", true, false);
      if (parserType == null) { Debug.Fail(""); return null; }
      System.Reflection.MethodInfo getTopElement =
 parserType.GetMethod("GetTopElement", BindingFlags.NonPublic|BindingFlags.Instance, null, new Type[]{}, null);
      if (getTopElement == null) { Debug.Fail(""); return null; }
      object parser =
 Activator.CreateInstance(parserType, BindingFlags.Instance|BindingFlags.NonPublic, null, new Object[]{this.serializedPermissions}, null);
      return (System.Security.SecurityElement)getTopElement.Invoke(parser, null);
#endif
        }

        protected PermissionSet InstantiatePermissionAttributes()
        {
            var permissions = new PermissionSet(PermissionState.None);
            var permissionAttributes = PermissionAttributes;
            for (int i = 0, n = permissionAttributes == null ? 0 : permissionAttributes.Count; i < n; i++)
            {
                //^ assert permissionAttributes != null;
                var result = GetPermissionOrSetOfPermissionsFromAttribute(permissionAttributes[i]);
                if (result == null) continue;
                if (result is PermissionSet)
                {
                    permissions = permissions.Union((PermissionSet)result);
                }
                else
                {
                    var permission = result as IPermission;
                    if (permission == null) continue;
                    permissions.AddPermission(permission);
                }
            }

            return permissions;
        }

        protected object GetPermissionOrSetOfPermissionsFromAttribute(AttributeNode attr)
        {
            if (attr == null) return null;
            var secAttr = attr.GetRuntimeAttribute() as Security.Permissions.SecurityAttribute;
            if (secAttr == null) return null;
            var pSetAttr = secAttr as PermissionSetAttribute;
            if (pSetAttr != null)
                return pSetAttr.CreatePermissionSet();
            return CreatePermission(secAttr);
        }

        private IPermission CreatePermission(Security.Permissions.SecurityAttribute /*!*/ secAttr)
        {
            //This could execute partially trusted code, so set up a very restrictive execution environment
            var perm = new PermissionSet(PermissionState.None);
            //TODO: add permissions if the attribute is from a trusted assembly
            perm.PermitOnly();
            try
            {
                return secAttr.CreatePermission();
            }
            catch
            {
            }

            return null;
        }
#endif
    }

    public struct Resource
    {
        public bool IsPublic { get; set; }

        public string Name { get; set; }

        public Module DefiningModule { get; set; }

        public byte[] Data { get; set; }
    }

    public struct Win32Resource
    {
        public string TypeName { get; set; }

        public int TypeId { get; set; }

        public string Name { get; set; }

        public int Id { get; set; }

        public int LanguageId { get; set; }

        public int CodePage { get; set; }

        public byte[] Data { get; set; }
    }
#if FxCop
  public class ModuleNode : Node, IDisposable{
#else
    public class Module : Node, IDisposable
    {
#endif
        internal Reader reader;

        public delegate void TypeNodeListProvider(Module /*!*/ module);

        protected TypeNodeListProvider provideTypeNodeList;

        public delegate TypeNode TypeNodeProvider(Identifier /*!*/ @namespace, Identifier /*!*/ name);

        protected TypeNodeProvider provideTypeNode;
        protected TrivialHashtable namespaceTable = new TrivialHashtable();
        protected NamespaceList namespaceList;
        protected int savedTypesLength;

        public delegate void CustomAttributeProvider(Module /*!*/ module);

        protected CustomAttributeProvider provideCustomAttributes;

        public delegate void ResourceProvider(Module /*!*/ module);

        protected ResourceProvider provideResources;

        public delegate AssemblyNode AssemblyReferenceResolver(AssemblyReference /*!*/ assemblyReference,
            Module /*!*/ referencingModule);

        public event AssemblyReferenceResolver AssemblyReferenceResolution;
        public event AssemblyReferenceResolver AssemblyReferenceResolutionAfterProbingFailed;
#if !NoXml
        public delegate XmlDocument DocumentationResolver(Module referencingModule);

        public event DocumentationResolver DocumentationResolution;
#endif
#if !MinimalReader
        public bool IsNormalized;
#endif
#if !NoWriter
        public bool UsePublicKeyTokensForAssemblyReferences = true;
#endif
        internal int FileAlignment = 512;
        internal static readonly object GlobalLock = new object();
#if !NoWriter
        public bool StripOptionalModifiersFromLocals = true;
#endif
#if FxCop
    public ModuleNode()
#else
        public Module()
#endif
            : base(NodeType.Module)
        {
#if !MinimalReader
            IsNormalized = false;
#endif
        }
#if FxCop
    public ModuleNode(TypeNodeProvider provider, TypeNodeListProvider listProvider, CustomAttributeProvider provideCustomAttributes, ResourceProvider provideResources)
#else
        public Module(TypeNodeProvider provider, TypeNodeListProvider listProvider,
            CustomAttributeProvider provideCustomAttributes, ResourceProvider provideResources)
#endif
            : base(NodeType.Module)
        {
            this.provideCustomAttributes = provideCustomAttributes;
            this.provideResources = provideResources;
            provideTypeNode = provider;
            provideTypeNodeList = listProvider;
#if !MinimalReader
            IsNormalized = true;
#endif
        }

        public virtual void Dispose()
        {
            if (reader != null) reader.Dispose();
            reader = null;
            var mrefs = moduleReferences;
            for (int i = 0, n = mrefs == null ? 0 : mrefs.Count; i < n; i++)
            {
                //^ assert mrefs != null;
                var mr = mrefs[i];
                if (mr != null && mr.Module == null) continue;
                mr.Module.Dispose();
            }

            moduleReferences = null;
        }

        public AssemblyReferenceList AssemblyReferences { get; set; }

        /// <summary>The assembly, if any, that includes this module in its ModuleReferences.</summary>
        public AssemblyNode ContainingAssembly { get; set; }

        public ushort DllCharacteristics { get; set; }

        public string Directory { get; set; }

        public AssemblyHashAlgorithm HashAlgorithm { get; set; } = AssemblyHashAlgorithm.SHA1;

        public byte[] HashValue { get; set; }

        /// <summary>An enumeration that indicates if the module is an executable, library or resource, and so on.</summary>
        public ModuleKindFlags Kind { get; set; }

        /// <summary>The path of the file from which this module or assembly was loaded or will be stored in.</summary>
        public string Location { get; set; }

        public Guid Mvid { get; set; }

        /// <summary>Identifies the version of the CLR that is required to load this module or assembly.</summary>
        public string TargetRuntimeVersion { get; set; }

        public int LinkerMajorVersion { get; set; } = 6;

        public int LinkerMinorVersion { get; set; }

        public int MetadataFormatMajorVersion { get; set; }

        public int MetadataFormatMinorVersion { get; set; }

        private bool? projectModule;

        /// <summary>
        ///     When true, then methods are projected based on the flags of their containing types.
        ///     When false, then no projection happens (i.e., it overrides the type flags).
        ///     Can be set by client, otherwise the default is that modules created from .winmd
        ///     files are projected, all others are not projected.
        /// </summary>
        public bool ProjectTypesContainedInModule
        {
            get
            {
                if (!projectModule.HasValue)
                    projectModule = Path.GetExtension(Location ?? "")
                        .Equals(".winmd", StringComparison.OrdinalIgnoreCase);
                return projectModule.Value;
            }
            set { projectModule = value; }
        }

        /// <summary>The name of the module or assembly. Includes the file extension if the module is not an assembly.</summary>
        public string Name { get; set; }

        public PEKindFlags PEKind { get; set; } = PEKindFlags.ILonly;

        public bool TrackDebugData { get; set; }
#if !FxCop
        /// <summary>
        ///     If any exceptions were encountered while reading in this module, they are recorded here. Since reading is lazy,
        ///     this list can grow dynamically during the use of a module.
        /// </summary>
        public ArrayList MetadataImportErrors { get; set; }
#endif
        protected AttributeList attributes;

        /// <summary>
        ///     The attributes associated with this module or assembly. This corresponds to C# custom attributes with the assembly
        ///     or module target specifier.
        /// </summary>
        public virtual AttributeList Attributes
        {
            get
            {
                if (attributes != null) return attributes;
                if (provideCustomAttributes != null)
                    lock (GlobalLock)
                    {
                        if (attributes == null)
                            provideCustomAttributes(this);
                    }
                else
                    attributes = new AttributeList();

                return attributes;
            }
            set { attributes = value; }
        }

        protected SecurityAttributeList securityAttributes;
        /// <summary>
        ///     Declarative security for the module or assembly.
        /// </summary>
        public virtual SecurityAttributeList SecurityAttributes
        {
            get
            {
                if (securityAttributes != null) return securityAttributes;
                if (provideCustomAttributes != null)
                {
                    var dummy = Attributes; //As a side effect, this.securityAttributes gets populated
                    if (dummy != null) dummy = null;
                }
                else
                {
                    securityAttributes = new SecurityAttributeList();
                }

                return securityAttributes;
            }
            set { securityAttributes = value; }
        }
#if !MinimalReader
        /// <summary>The source code, if any, corresponding to the value in Documentation.</summary>
        public Node DocumentationNode;
#endif
#if !NoXml
        protected XmlDocument documentation;

        /// <summary>
        ///     An XML Document Object Model for a document containing all of the documentation comments applicable to members
        ///     defined in this module.
        /// </summary>
        public virtual XmlDocument Documentation
        {
            get
            {
                var documentation = this.documentation;
                if (documentation != null) return documentation;
                if (DocumentationResolution != null)
                    documentation = this.documentation = DocumentationResolution(this);
                if (documentation != null) return documentation;
                XmlDocument doc = null;
                if (Directory != null && Name != null)
                {
                    var fileName = Name + ".xml";
                    var cc = CultureInfo.CurrentUICulture;
                    while (cc != null && cc != CultureInfo.InvariantCulture)
                    {
                        doc = ProbeForXmlDocumentation(Directory, cc.Name, fileName);
                        if (doc != null) break;
                        cc = cc.Parent;
                    }

                    if (doc == null)
                        doc = ProbeForXmlDocumentation(Directory, null, fileName);
                }

                if (doc == null)
                {
                    doc = new XmlDocument();
                    doc.XmlResolver = null;
                }

                return this.documentation = doc;
            }
            set { documentation = value; }
        }

        public virtual XmlDocument ProbeForXmlDocumentation(string dir, string subDir, string fileName)
        {
            try
            {
                if (dir == null || fileName == null) return null;
                if (subDir != null) dir = Path.Combine(dir, subDir);
                var docFileName = Path.Combine(dir, fileName);
                if (File.Exists(docFileName))
                {
                    var doc = new XmlDocument();
                    doc.XmlResolver = null;
                    using (var reader = new XmlTextReader(docFileName))
                    {
                        reader.DtdProcessing = DtdProcessing.Prohibit;
                        doc.Load(reader);
                        return doc;
                    }
                }
            }
            catch (Exception e)
            {
                if (MetadataImportErrors == null) MetadataImportErrors = new ArrayList();
                MetadataImportErrors.Add(e);
            }

            return null;
        }
#endif
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        protected internal static readonly Method NoSuchMethod = new Method();

        protected Method entryPoint;

        /// <summary>
        ///     If this module is an executable, this method is the one that gets called to start the execution of managed
        ///     code.
        /// </summary>
        public virtual Method EntryPoint
        {
            get
            {
                if (entryPoint == null)
                {
                    if (provideCustomAttributes != null)
                    {
                        var dummy = Attributes; //Gets the entry point as a side effect
                        if (dummy != null) dummy = null;
                    }
                    else
                    {
                        entryPoint = NoSuchMethod;
                    }
                }

                if (entryPoint == NoSuchMethod) return null;
                return entryPoint;
            }
            set { entryPoint = value; }
        }

        protected ModuleReferenceList moduleReferences;
        /// <summary>The list of modules (excluding assemblies) defining members that are referred to in this module or assembly.</summary>
        public ModuleReferenceList ModuleReferences
        {
            get
            {
                //Populating the type list may cause module references to be added
                if (Types == null) return moduleReferences;
                return moduleReferences;
            }
            set { moduleReferences = value; }
        }
#if !MinimalReader
        public virtual bool ContainsModule(Module module)
        {
            if (module == null || ModuleReferences == null || ModuleReferences.Count == 0) return false;
            var n = ModuleReferences.Count;
            for (var i = 0; i < n; ++i)
            {
                var mr = ModuleReferences[i];
                if (mr == null) continue;
                if (mr.Module == module)
                    return true;
            }

            return false;
        }
#endif
        protected ResourceList resources;

        /// <summary>
        ///     A list of managed resources linked or embedded into this module or assembly.
        /// </summary>
        public virtual ResourceList Resources
        {
            get
            {
                if (resources != null) return resources;
                if (provideResources != null)
                    lock (GlobalLock)
                    {
                        if (resources == null)
                            provideResources(this);
                    }
                else
                    resources = new ResourceList();

                return resources;
            }
            set { resources = value; }
        }

        protected Win32ResourceList win32Resources;
        /// <summary>
        ///     A list of Win32 resources embedded in this module or assembly.
        /// </summary>
        public virtual Win32ResourceList Win32Resources
        {
            get
            {
                if (win32Resources != null) return win32Resources;
                if (provideResources != null)
                {
                    var dummy = Resources; //gets the win32 resources as as side effect
                    if (dummy != null) dummy = null;
                }
                else
                {
                    win32Resources = new Win32ResourceList();
                }

                return win32Resources;
            }
            set { win32Resources = value; }
        }
#if !NoWriter
        public virtual void AddWin32ResourceFile(string win32ResourceFilePath)
        {
            if (win32ResourceFilePath == null) return;
            Writer.AddWin32ResourceFileToModule(this, win32ResourceFilePath);
        }

        public virtual void AddWin32ResourceFile(Stream win32ResourceStream)
        {
            if (win32ResourceStream == null) return;
            Writer.AddWin32ResourceFileToModule(this, win32ResourceStream);
        }

        public virtual void AddWin32Icon(string win32IconFilePath)
        {
            if (win32IconFilePath == null) return;
            Writer.AddWin32Icon(this, win32IconFilePath);
        }

        public virtual void AddWin32Icon(Stream win32IconStream)
        {
            Writer.AddWin32Icon(this, win32IconStream);
        }

        public void AddWin32VersionInfo(CompilerOptions options)
        {
            if (options == null) return;
            Writer.AddWin32VersionInfo(this, options);
        }
#endif
        /// <summary>
        ///     Gets the first attribute of the given type in the custom attribute list of this module. Returns null if none found.
        ///     This should not be called until the module has been processed to replace symbolic references
        ///     to members with references to the actual members.
        /// </summary>
        public virtual AttributeNode GetAttribute(TypeNode attributeType)
        {
            var attributes = GetAttributes(attributeType, 1);
            if (attributes != null && attributes.Count > 0)
                return attributes[0];
            return null;
        }

        public virtual AttributeList GetAttributes(TypeNode attributeType)
        {
            return GetAttributes(attributeType, int.MaxValue);
        }

        public virtual AttributeList GetAttributes(TypeNode attributeType, int maxCount)
        {
            var foundAttributes = new AttributeList();
            if (attributeType == null) return foundAttributes;
            var attributes = Attributes;
            for (int i = 0, count = 0, n = attributes == null ? 0 : attributes.Count; i < n && count < maxCount; i++)
            {
                var attr = attributes[i];
                if (attr == null) continue;
                var mb = attr.Constructor as MemberBinding;
                if (mb != null)
                {
                    if (mb.BoundMember == null) continue;
                    if (mb.BoundMember.DeclaringType != attributeType) continue;
                    foundAttributes.Add(attr);
                    count++;
                    continue;
                }

                var lit = attr.Constructor as Literal;
                if (lit == null) continue;
                if (lit.Value as TypeNode != attributeType) continue;
                foundAttributes.Add(attr);
                count++;
            }

            return foundAttributes;
        }
#if !NoXml
        protected TrivialHashtable memberDocumentationCache;
        public TrivialHashtable GetMemberDocumentationCache()
        {
            var cache = memberDocumentationCache;
            if (cache != null) return cache;
            lock (this)
            {
                if (memberDocumentationCache != null) return memberDocumentationCache;
                var doc = Documentation;
                if (doc == null && ContainingAssembly != null && ContainingAssembly != this)
                    return memberDocumentationCache = ContainingAssembly.memberDocumentationCache;
                cache = memberDocumentationCache = new TrivialHashtable();
                if (doc == null) return cache;
                XmlNode docElem = doc.DocumentElement;
                if (docElem == null) return cache;
                XmlNode membersNode = null;
                if (docElem.HasChildNodes)
                    foreach (XmlNode dec in docElem.ChildNodes)
                        if (dec.Name == "members")
                        {
                            membersNode = dec;
                            break;
                        }

                if (membersNode == null) return cache;
                if (membersNode.HasChildNodes)
                    foreach (XmlNode member in membersNode.ChildNodes)
                    {
                        if (member.Name != "member") continue;
                        var nameAttr = member.Attributes.GetNamedItem("name");
                        if (nameAttr == null) continue;
                        cache[Identifier.For(nameAttr.Value).UniqueIdKey] = member;
                    }

                return cache;
            }
        }
#endif
        protected TrivialHashtable validNamespaces;

        public NamespaceList GetNamespaceList()
        {
            if (reader != null) return GetNamespaceListFromReader();
#if !MinimalReader
            var types = Types;
            var n = types == null ? 0 : types.Count;
            if (namespaceList == null || n > savedTypesLength)
                lock (this)
                {
                    if (namespaceList != null && this.types != null && this.types.Count == savedTypesLength)
                        return namespaceList;
                    var nsList = namespaceList = new NamespaceList();
                    var nsTable = validNamespaces = new TrivialHashtable();
                    for (var i = 0; i < n; i++)
                    {
                        //^ assert this.types != null;
                        var t = this.types[i];
                        if (t == null) continue;
                        if (t.Namespace == null) t.Namespace = Identifier.Empty;
                        var ns = nsTable[t.Namespace.UniqueIdKey] as Namespace;
                        if (ns != null)
                        {
                            if (t.IsPublic) ns.isPublic = true;
                            ns.Types.Add(t);
                            continue;
                        }

                        ns = new Namespace(t.Namespace);
                        ns.isPublic = t.IsPublic;
                        ns.Types = new TypeNodeList();
                        ns.Types.Add(t);
                        nsTable[t.Namespace.UniqueIdKey] = ns;
                        nsList.Add(ns);
                    }
                }
#endif
            return namespaceList;
        }

        private NamespaceList GetNamespaceListFromReader()
            //^ requires this.reader != null;
        {
            if (namespaceList == null)
                lock (GlobalLock)
                {
                    reader.GetNamespaces();
                    var nsList = namespaceList = reader.namespaceList;
                    var nsTable = validNamespaces = new TrivialHashtable();
                    for (int i = 0, n = nsList == null ? 0 : nsList.Count; i < n; i++)
                    {
                        //^ assert nsList != null;
                        var ns = nsList[i];
                        if (ns == null || ns.Name == null) continue;
                        ns.ProvideTypes = GetTypesForNamespace;
                        nsTable[ns.Name.UniqueIdKey] = ns;
                    }
                }

            return namespaceList;
        }

        private void GetTypesForNamespace(Namespace nspace, object handle)
        {
            if (nspace == null || nspace.Name == null) return;
            lock (GlobalLock)
            {
                var key = nspace.Name.UniqueIdKey;
                var types = Types;
                var nsTypes = nspace.Types = new TypeNodeList();
                for (int i = 0, n = types == null ? 0 : types.Count; i < n; i++)
                {
                    var t = types[i];
                    if (t == null || t.Namespace == null) continue;
                    if (t.Namespace.UniqueIdKey == key) nsTypes.Add(t);
                }
            }
        }

        public bool IsValidNamespace(Identifier nsName)
        {
            if (nsName == null) return false;
            GetNamespaceList();
            //^ assert this.validNamespaces != null;
            return validNamespaces[nsName.UniqueIdKey] != null;
        }

        public bool IsValidTypeName(Identifier nsName, Identifier typeName)
        {
            if (nsName == null || typeName == null) return false;
            if (!IsValidNamespace(nsName)) return false;
            if (reader != null) return reader.IsValidTypeName(nsName, typeName);
            return GetType(nsName, typeName) != null;
        }

        public Module GetNestedModule(string moduleName)
        {
            if (Types == null) Debug.Assert(false);
            var moduleReferences = ModuleReferences; //This should now contain all interesting referenced modules
            for (int i = 0, n = moduleReferences == null ? 0 : moduleReferences.Count; i < n; i++)
            {
                var mref = moduleReferences[i];
                if (mref == null) continue;
                if (mref.Name == moduleName) return mref.Module;
            }

            return null;
        }

        internal TrivialHashtableUsingWeakReferences /*!*/ StructurallyEquivalentType
        {
            get
            {
                if (structurallyEquivalentType == null)
                    structurallyEquivalentType = new TrivialHashtableUsingWeakReferences();
                return structurallyEquivalentType;
            }
        }

        private TrivialHashtableUsingWeakReferences structurallyEquivalentType;

        /// <summary>
        ///     The identifier represents the structure via some mangling scheme. The result can be either from this module,
        ///     or any module this module has a reference to.
        /// </summary>
        public virtual TypeNode GetStructurallyEquivalentType(Identifier ns, Identifier /*!*/ id)
        {
            return GetStructurallyEquivalentType(ns, id, id, true);
        }

        public virtual TypeNode TryGetTemplateInstance(Identifier uniqueMangledName)
        {
            var result = (TypeNode)StructurallyEquivalentType[uniqueMangledName.UniqueIdKey];
            if (result == Class.DoesNotExist) return null;
            return result;
        }

        public virtual TypeNode GetStructurallyEquivalentType(Identifier ns, Identifier /*!*/ id,
            Identifier uniqueMangledName, bool lookInReferencedAssemblies)
        {
            if (uniqueMangledName == null) uniqueMangledName = id;
            var result = (TypeNode)StructurallyEquivalentType[uniqueMangledName.UniqueIdKey];
            if (result == Class.DoesNotExist) return null;
            if (result != null) return result;
            lock (GlobalLock)
            {
                result = GetType(ns, id);
                if (result != null)
                {
                    StructurallyEquivalentType[uniqueMangledName.UniqueIdKey] = result;
                    return result;
                }

                if (!lookInReferencedAssemblies)
                    goto notfound;
                var refs = AssemblyReferences;
                for (int i = 0, n = refs == null ? 0 : refs.Count; i < n; i++)
                {
                    var ar = refs[i];
                    if (ar == null) continue;
                    var a = ar.Assembly;
                    if (a == null) continue;
                    result = a.GetType(ns, id);
                    if (result != null)
                    {
                        StructurallyEquivalentType[uniqueMangledName.UniqueIdKey] = result;
                        return result;
                    }
                }

                notfound:
                StructurallyEquivalentType[uniqueMangledName.UniqueIdKey] = Class.DoesNotExist;
                return null;
            }
        }

        public virtual TypeNode GetType(Identifier @namespace, Identifier name, bool lookInReferencedAssemblies)
        {
            return GetType(@namespace, name, lookInReferencedAssemblies,
                lookInReferencedAssemblies ? new TrivialHashtable() : null);
        }

        protected virtual TypeNode GetType(Identifier @namespace, Identifier name, bool lookInReferencedAssemblies,
            TrivialHashtable assembliesAlreadyVisited)
        {
            if (assembliesAlreadyVisited != null)
            {
                if (assembliesAlreadyVisited[UniqueKey] != null) return null;
                assembliesAlreadyVisited[UniqueKey] = this;
            }

            var result = GetType(@namespace, name);
            if (result != null || !lookInReferencedAssemblies) return result;
            var refs = AssemblyReferences;
            for (int i = 0, n = refs == null ? 0 : refs.Count; i < n; i++)
            {
                var ar = refs[i];
                if (ar == null) continue;
                var a = ar.Assembly;
                if (a == null) continue;
                result = a.GetType(@namespace, name, true, assembliesAlreadyVisited);
                if (result != null) return result;
            }

            return null;
        }

        public virtual TypeNode GetType(Identifier @namespace, Identifier name)
        {
            if (@namespace == null || name == null) return null;
            TypeNode result = null;
            if (namespaceTable == null) namespaceTable = new TrivialHashtable();
            var nsTable = (TrivialHashtable)namespaceTable[@namespace.UniqueIdKey];
            if (nsTable != null)
            {
                result = (TypeNode)nsTable[name.UniqueIdKey];
                if (result == Class.DoesNotExist) return null;
                if (result != null) return result;
            }
            else
            {
                lock (GlobalLock)
                {
                    nsTable = (TrivialHashtable)namespaceTable[@namespace.UniqueIdKey];
                    if (nsTable == null)
                        namespaceTable[@namespace.UniqueIdKey] = nsTable = new TrivialHashtable(32);
                }
            }

            if (provideTypeNode != null)
                lock (GlobalLock)
                {
                    result = (TypeNode)nsTable[name.UniqueIdKey];
                    if (result == Class.DoesNotExist) return null;
                    if (result != null) return result;
                    result = provideTypeNode(@namespace, name);
                    if (result != null)
                    {
                        nsTable[name.UniqueIdKey] = result;
                        return result;
                    }

                    nsTable[name.UniqueIdKey] = Class.DoesNotExist;
                    return null;
                }

            if (types != null && types.Count > savedTypesLength)
            {
                var n = savedTypesLength = types.Count;
                for (var i = 0; i < n; i++)
                {
                    var t = types[i];
                    if (t == null) continue;
                    if (t.Namespace == null) t.Namespace = Identifier.Empty;
                    nsTable = (TrivialHashtable)namespaceTable[t.Namespace.UniqueIdKey];
                    if (nsTable == null) namespaceTable[t.Namespace.UniqueIdKey] = nsTable = new TrivialHashtable();
                    nsTable[t.Name.UniqueIdKey] = t;
                }

                return GetType(@namespace, name);
            }

            return null;
        }

        protected internal TypeNodeList types;
        /// <summary>The types contained in this module or assembly.</summary>
        public virtual TypeNodeList Types
        {
            get
            {
#if CodeContracts
                Contract.Ensures(Contract.Result<TypeNodeList>() != null);

#endif
                if (types != null) return types;
                if (provideTypeNodeList != null)
                    lock (GlobalLock)
                    {
                        if (types == null)
                            provideTypeNodeList(this);
                    }
                else
                    types = new TypeNodeList();

                return types;
            }
            set { types = value; }
        }
#if !MinimalReader
        protected TrivialHashtable referencedModulesAndAssemblies;
#endif
        public virtual bool HasReferenceTo(Module module)
        {
            if (module == null) return false;
            var assembly = module as AssemblyNode;
            if (assembly != null)
            {
                var arefs = AssemblyReferences;
                for (int i = 0, n = arefs == null ? 0 : arefs.Count; i < n; i++)
                {
                    var aref = arefs[i];
                    if (aref == null) continue;
                    if (aref.Matches(assembly.Name, assembly.Version, assembly.Culture, assembly.PublicKeyToken))
                        return true;
                }
            }

            if (ContainingAssembly != module.ContainingAssembly)
                return false;
            var mrefs = ModuleReferences;
            for (int i = 0, n = mrefs == null ? 0 : mrefs.Count; i < n; i++)
            {
                //^ assert mrefs != null;
                var mref = mrefs[i];
                if (mref == null || mref.Name == null) continue;
                if (0 == PlatformHelpers.StringCompareOrdinalIgnoreCase(mref.Name, module.Name))
                    return true;
            }

            return false;
        }

        internal void InitializeAssemblyReferenceResolution(Module referringModule)
        {
            if (AssemblyReferenceResolution == null && referringModule != null)
            {
                AssemblyReferenceResolution = referringModule.AssemblyReferenceResolution;
                AssemblyReferenceResolutionAfterProbingFailed =
                    referringModule.AssemblyReferenceResolutionAfterProbingFailed;
            }
        }
#if !MinimalReader
        public static Module GetModule(byte[] buffer)
        {
            return GetModule(buffer, null, false, false, true, false);
        }

        public static Module GetModule(byte[] buffer, IDictionary cache)
        {
            return GetModule(buffer, null, false, false, false, false);
        }

        public static Module GetModule(byte[] buffer, IDictionary cache, bool doNotLockFile, bool getDebugInfo,
            bool useGlobalCache)
        {
            return GetModule(buffer, cache, doNotLockFile, getDebugInfo, useGlobalCache, false);
        }

        public static Module GetModule(byte[] buffer, IDictionary cache, bool doNotLockFile, bool getDebugInfo,
            bool useGlobalCache, bool preserveShortBranches)
        {
            if (buffer == null) return null;
            return new Reader(buffer, cache, doNotLockFile, getDebugInfo, useGlobalCache, false).ReadModule();
        }
#endif
        public static Module GetModule(string location)
        {
            return GetModule(location, null, false, false, true, false);
        }

        public static Module GetModule(string location, bool doNotLockFile, bool getDebugInfo, bool useGlobalCache)
        {
            return GetModule(location, null, doNotLockFile, getDebugInfo, useGlobalCache, false);
        }

        public static Module GetModule(string location, IDictionary cache)
        {
            return GetModule(location, cache, false, false, false, false);
        }

        public static Module GetModule(string location, IDictionary cache, bool doNotLockFile, bool getDebugInfo,
            bool useGlobalCache)
        {
            return GetModule(location, cache, doNotLockFile, getDebugInfo, useGlobalCache, false);
        }

        public static Module GetModule(string location, IDictionary cache, bool doNotLockFile, bool getDebugInfo,
            bool useGlobalCache, bool preserveShortBranches)
        {
            if (location == null) return null;
            return new Reader(location, cache, doNotLockFile, getDebugInfo, useGlobalCache, preserveShortBranches)
                .ReadModule();
        }

        public virtual AssemblyNode Resolve(AssemblyReference assemblyReference)
        {
            if (AssemblyReferenceResolution == null) return null;
            return AssemblyReferenceResolution(assemblyReference, this);
        }

        public virtual AssemblyNode ResolveAfterProbingFailed(AssemblyReference assemblyReference)
        {
            if (AssemblyReferenceResolutionAfterProbingFailed == null) return null;
            return AssemblyReferenceResolutionAfterProbingFailed(assemblyReference, this);
        }
#if !NoWriter
        public virtual void WriteModule(string /*!*/ location, bool writeDebugSymbols)
        {
            Location = location;
            Writer.WritePE(location, writeDebugSymbols, this);
        }

        public virtual void WriteModule(Stream /*!*/ executable, Stream debugSymbols)
        {
            Writer.WritePE(executable, debugSymbols, this);
        }

        public virtual void WriteModule(out byte[] executable)
        {
            Writer.WritePE(out executable, this);
        }

        public virtual void WriteModule(out byte[] executable, out byte[] debugSymbols)
        {
            Writer.WritePE(out executable, out debugSymbols, this);
        }

        public virtual void WriteModule(string /*!*/ location, CompilerParameters /*!*/ options)
        {
            Location = location;
            Writer.WritePE(options, this);
        }
#endif
#if !NoXml
        public virtual void WriteDocumentation(TextWriter doc)
        {
            if (documentation == null) return;
            var xwriter = new XmlTextWriter(doc);
            xwriter.Formatting = Formatting.Indented;
            xwriter.Indentation = 2;
            xwriter.WriteProcessingInstruction("xml", "version=\"1.0\"");
            xwriter.WriteStartElement("doc");
            var assem = this as AssemblyNode;
            if (assem != null)
            {
                xwriter.WriteStartElement("assembly");
                xwriter.WriteElementString("name", assem.Name);
                xwriter.WriteEndElement();
            }

            xwriter.WriteStartElement("members");
            var types = Types;
            for (int i = 1, n = types == null ? 0 : types.Count; i < n; i++)
            {
                //^ assert types != null;
                var t = types[i];
                if (t == null) continue;
                t.WriteDocumentation(xwriter);
            }

            xwriter.WriteEndElement();
            xwriter.WriteEndElement();
            xwriter.Close();
        }
#endif
#if !NoWriter
        public delegate MethodBodySpecializer /*!*/
            MethodBodySpecializerFactory(Module /*!*/ m, TypeNodeList /*!*/ pars, TypeNodeList /*!*/ args);

        public MethodBodySpecializerFactory CreateMethodBodySpecializer;
        public MethodBodySpecializer /*!*/ GetMethodBodySpecializer(TypeNodeList /*!*/ pars, TypeNodeList /*!*/ args)
        {
            if (CreateMethodBodySpecializer != null)
                return CreateMethodBodySpecializer(this, pars, args);
            return new MethodBodySpecializer(this, pars, args);
        }
#endif
    }

    public class AssemblyNode : Module
    {
        //An assembly is a module with a strong name
#if !NoWriter
        public string KeyContainerName;
        public byte[] KeyBlob;
#endif
#if !NoReflection
        private static Hashtable
            CompiledAssemblies; // so we can find in-memory compiled assemblies later (contains weak references)
#endif
#if !MinimalReader
        protected AssemblyNode contractAssembly;
        /// <summary>A separate assembly that supplied the type and method contracts for this assembly.</summary>
        public virtual AssemblyNode ContractAssembly
        {
            get { return contractAssembly; }
            set
            {
                if (contractAssembly != null)
                {
                    Debug.Assert(false);
                    return;
                }

                contractAssembly = value;
                if (value == null) return;

                #region Copy over any external references from the contract assembly to this one (if needed)

                // These external references are needed only for the contract deserializer
                var ars = new AssemblyReferenceList();
                var contractReferences = value.AssemblyReferences;
                // see if contractReferences[i] is already in the external references of "this"
                for (int i = 0, n = contractReferences == null ? 0 : contractReferences.Count; i < n; i++)
                {
                    //^ assert contractReferences != null;
                    var aref = contractReferences[i];
                    if (aref == null) continue;
                    if (aref.Assembly != this)
                    {
                        // don't copy the contract's external reference to "this"
                        var j = 0;
                        var m = AssemblyReferences == null ? 0 : AssemblyReferences.Count;
                        while (j < m)
                        {
                            if (aref.Assembly.Name != null &&
                                AssemblyReferences[j].Name != null &&
                                aref.Assembly.Name.Equals(AssemblyReferences[j].Name))
                                break;
                            j++;
                        }

                        if (j == m) // then it wasn't found in the list of the real references
                            ars.Add(contractReferences[i]);
                    }
                }

                if (AssemblyReferences == null)
                    AssemblyReferences = new AssemblyReferenceList();
                for (int i = 0, n = ars.Count; i < n; i++) AssemblyReferences.Add(ars[i]);

                #endregion Copy over any external references from the contract assembly to this one (if needed)

#if ExtendedRuntime
        #region Copy over any assembly-level attributes from the Contracts namespace
        int contractsNamespaceKey = SystemTypes.NonNullType.Namespace.UniqueIdKey;
        // Copy the assembly-level contract attributes over to the shadowed assembly.
        foreach(AttributeNode attr in contractAssembly.Attributes) {
          if(attr.Type != SystemTypes.ShadowsAssemblyAttribute // can't copy this one or the real assembly will be treated as a shadow assembly!
            &&
            attr.Type.Namespace != null && attr.Type.Namespace.UniqueIdKey == contractsNamespaceKey)
            this.Attributes.Add(attr);
        }
        #endregion Copy over any assembly-level attributes from the Contracts namespace
#endif

                TypeNodeList instantiatedTypes = null;
                if (reader != null) instantiatedTypes = reader.GetInstantiatedTypes();
                if (instantiatedTypes != null)
                    for (int i = 0, n = instantiatedTypes.Count; i < n; i++)
                    {
                        var t = instantiatedTypes[i];
                        if (t == null) continue;

                        if (t.members == null)
                        {
#if ExtendedRuntime
              // Then will never get to ApplyOutOfBandContracts and will never have any
              // type-level attributes copied over. So need to do this here as well as
              // within ApplyOutOfBandContracts
              TypeNode contractType = this.ContractAssembly.GetType(t.Namespace, t.Name);
              if (contractType == null) continue;
              // Copy the type-level contract attributes over to the shadowed type.
              foreach (AttributeNode attr in contractType.Attributes) {
                if (attr.Type.Namespace != null && attr.Type.Namespace.UniqueIdKey == contractsNamespaceKey)
                  t.Attributes.Add(attr);
              }
#endif
                        }
#if ExtendedRuntime
            t.ApplyOutOfBandContracts();
#endif
                    }
            }
        }
#endif
        internal static readonly AssemblyNode /*!*/
            Dummy = new AssemblyNode();

        protected string strongName;

        /// <summary>
        ///     A string containing the name, version, culture and key of this assembly, formatted as required by the CLR loader.
        /// </summary>
        public virtual string /*!*/ StrongName
        {
            get
            {
                if (strongName == null)
                    strongName = GetStrongName(Name, Version, Culture, PublicKeyOrToken,
                        (Flags & AssemblyFlags.Retargetable) != 0);
                return strongName;
            }
        }

        [Obsolete("Please use GetAttribute(TypeNode attributeType)")]
        public virtual AttributeNode GetAttributeByName(TypeNode attributeType)
        {
            if (attributeType == null) return null;
            var attributes = Attributes;
            for (int i = 0, n = attributes == null ? 0 : attributes.Count; i < n; i++)
            {
                //^ assert attributes != null;
                var attr = attributes[i];
                if (attr == null) continue;
                var mb = attr.Constructor as MemberBinding;
                if (mb != null)
                {
                    if (mb.BoundMember == null || mb.BoundMember.DeclaringType == null) continue;
                    if (mb.BoundMember.DeclaringType.FullName != attributeType.FullName) continue;
                    return attr;
                }
            }

            return null;
        }

        /// <summary>
        ///     Gets the first attribute of the given type in the custom attribute list of this member. Returns null if none found.
        ///     The member is assumed to be either imported, or already in a form suitable for export.
        /// </summary>
        public virtual AttributeNode GetModuleAttribute(TypeNode attributeType)
        {
            if (attributeType == null) return null;
            var attributes = ModuleAttributes;
            for (int i = 0, n = attributes == null ? 0 : attributes.Count; i < n; i++)
            {
                //^ assert attributes != null;
                var attr = attributes[i];
                if (attr == null) continue;
                var mb = attr.Constructor as MemberBinding;
                if (mb != null)
                {
                    if (mb.BoundMember == null) continue;
                    if (mb.BoundMember.DeclaringType != attributeType) continue;
                    return attr;
                }

                var lit = attr.Constructor as Literal;
                if (lit == null) continue;
                if (lit.Value as TypeNode != attributeType) continue;
                return attr;
            }

            return null;
        }

        public AssemblyNode()
        {
            NodeType = NodeType.Assembly;
            ContainingAssembly = this;
        }

        public AssemblyNode(TypeNodeProvider provider, TypeNodeListProvider listProvider,
            CustomAttributeProvider provideCustomAttributes, ResourceProvider provideResources, string directory)
            : base(provider, listProvider, provideCustomAttributes, provideResources)
        {
            Directory = directory;
            NodeType = NodeType.Assembly;
            ContainingAssembly = this;
        }

        public override void Dispose()
        {
#if !NoReflection
            if (cachedRuntimeAssembly != null)
                cachedRuntimeAssembly.Dispose();
            cachedRuntimeAssembly = null;
#endif
            lock (Reader.StaticAssemblyCache)
            {
                foreach (var key in new ArrayList(Reader.StaticAssemblyCache.Keys))
                    if (Reader.StaticAssemblyCache[key] == this)
                        Reader.StaticAssemblyCache.Remove(key);
                if (TargetPlatform.AssemblyReferenceForInitialized)
                {
                    var aRef = (AssemblyReference)TargetPlatform.AssemblyReferenceFor[Identifier.For(Name).UniqueIdKey];
                    if (aRef != null && aRef.Assembly == this) aRef.Assembly = null;
                    //TODO: what about other static references to the assembly, such as SystemTypes.SystemXmlAssembly?
                }
            }

            base.Dispose();
        }

        /// <summary>The target culture of any localized resources in this assembly.</summary>
        public string Culture { get; set; }

        /// <summary>An enumeration that identifies the what kind of assembly this is.</summary>
        public AssemblyFlags Flags { get; set; }

        /// <summary>Attributes that specifically target a module rather an assembly.</summary>
        public string ModuleName
        {
            //An assembly can have a different name from the module.
            get;
            set;
        }

        /// <summary>The public part of the key pair used to sign this assembly, or a hash of the public key.</summary>
        public byte[] PublicKeyOrToken { get; set; }

        /// <summary>The version of this assembly.</summary>
        public Version Version { get; set; }

        public DateTime FileLastWriteTimeUtc { get; set; }

        protected TypeNodeList exportedTypes;

        /// <summary>
        ///     Public types defined in other modules making up this assembly and to which other assemblies may refer to.
        /// </summary>
        public virtual TypeNodeList ExportedTypes
        {
            get
            {
                if (exportedTypes != null) return exportedTypes;
                if (provideTypeNodeList != null)
                {
                    var types = Types; //Gets the exported types as a side-effect
                    if (types != null) types = null;
                }
                else
                {
                    exportedTypes = new TypeNodeList();
                }

                return exportedTypes;
            }
            set { exportedTypes = value; }
        }

        public bool GetDebugSymbols
        {
            get
            {
                if (reader == null) return false;
                return reader.getDebugSymbols;
            }
            set
            {
                if (reader == null) return;
                reader.getDebugSymbols = value;
            }
        }
#if !MinimalReader
        public static AssemblyNode GetAssembly(byte[] buffer)
        {
            return GetAssembly(buffer, null, false, false, true, false);
        }

        public static AssemblyNode GetAssembly(byte[] buffer, IDictionary cache)
        {
            return GetAssembly(buffer, cache, false, false, false, false);
        }

        public static AssemblyNode GetAssembly(byte[] buffer, IDictionary cache, bool doNotLockFile, bool getDebugInfo,
            bool useGlobalCache)
        {
            return GetAssembly(buffer, cache, doNotLockFile, getDebugInfo, useGlobalCache, false);
        }

        public static AssemblyNode GetAssembly(byte[] buffer, IDictionary cache, bool doNotLockFile, bool getDebugInfo,
            bool useGlobalCache, bool preserveShortBranches)
        {
            if (buffer == null) return null;
            if (CoreSystemTypes.SystemAssembly == null) Debug.Fail("");
            return new Reader(buffer, cache, doNotLockFile, getDebugInfo, useGlobalCache, preserveShortBranches)
                .ReadModule() as AssemblyNode;
        }
#endif
        public static AssemblyNode GetAssembly(string location)
        {
            return GetAssembly(location, null, false, false, true, false);
        }

        public static AssemblyNode GetAssembly(string location, bool doNotLockFile, bool getDebugInfo,
            bool useGlobalCache)
        {
            return GetAssembly(location, null, doNotLockFile, getDebugInfo, useGlobalCache, false);
        }

        public static AssemblyNode GetAssembly(string location, IDictionary cache)
        {
            return GetAssembly(location, cache, false, false, false, false);
        }

        public static AssemblyNode GetAssembly(string location, IDictionary cache, bool doNotLockFile,
            bool getDebugInfo, bool useGlobalCache)
        {
            return GetAssembly(location, cache, doNotLockFile, getDebugInfo, useGlobalCache, false);
        }
#if !MinimalReader
        /// <summary>
        ///     Calls the post load event only if the assembly is not already loaded/found in the cache.
        /// </summary>
        public static AssemblyNode GetAssembly(string location, bool doNotLockFile, bool getDebugInfo,
            bool useGlobalCache, PostAssemblyLoadProcessor postLoadEvent)
        {
            return GetAssembly(location, null, doNotLockFile, getDebugInfo, useGlobalCache, false, postLoadEvent);
        }

        public static AssemblyNode GetAssembly(string location, IDictionary cache, bool doNotLockFile,
            bool getDebugInfo, bool useGlobalCache, PostAssemblyLoadProcessor postLoadEvent)
        {
            return GetAssembly(location, cache, doNotLockFile, getDebugInfo, useGlobalCache, false, postLoadEvent);
        }

        public static AssemblyNode GetAssembly(string location, IDictionary cache, bool doNotLockFile,
            bool getDebugInfo, bool useGlobalCache, bool preserveShortBranches)
        {
            return GetAssembly(location, cache, doNotLockFile, getDebugInfo, useGlobalCache, preserveShortBranches,
                null);
        }

        public static AssemblyNode GetAssembly(string location, IDictionary cache, bool doNotLockFile,
            bool getDebugInfo, bool useGlobalCache, bool preserveShortBranches, PostAssemblyLoadProcessor postLoadEvent)
        {
            if (location == null) return null;
            if (CoreSystemTypes.SystemAssembly == null) Debug.Fail("");
            return new Reader(location, cache, doNotLockFile, getDebugInfo, useGlobalCache, preserveShortBranches)
                .ReadModule(postLoadEvent) as AssemblyNode;
        }
#else
    public static AssemblyNode GetAssembly(string location, IDictionary cache, bool doNotLockFile, bool getDebugInfo, bool useGlobalCache, bool preserveShortBranches)
    {
      if (location == null) return null;
      if (CoreSystemTypes.SystemAssembly == null) Debug.Fail("");
      return (new Reader(location, cache, doNotLockFile, getDebugInfo, useGlobalCache, preserveShortBranches)).ReadModule() as AssemblyNode;
    }
#endif

#if !MinimalReader || !NoXml || !NoData
        public static AssemblyNode GetAssembly(AssemblyReference assemblyReference)
        {
            return GetAssembly(assemblyReference, null, false, false, true, false);
        }

        public static AssemblyNode GetAssembly(AssemblyReference assemblyReference, bool doNotLockFile,
            bool getDebugInfo, bool useGlobalCache)
        {
            return GetAssembly(assemblyReference, null, doNotLockFile, getDebugInfo, useGlobalCache, false);
        }

        public static AssemblyNode GetAssembly(AssemblyReference assemblyReference, IDictionary cache)
        {
            return GetAssembly(assemblyReference, cache, false, false, false, false);
        }

        public static AssemblyNode GetAssembly(AssemblyReference assemblyReference, IDictionary cache,
            bool doNotLockFile, bool getDebugInfo, bool useGlobalCache)
        {
            return GetAssembly(assemblyReference, cache, doNotLockFile, getDebugInfo, useGlobalCache, false);
        }

        public static AssemblyNode GetAssembly(AssemblyReference assemblyReference, IDictionary cache,
            bool doNotLockFile, bool getDebugInfo, bool useGlobalCache, bool preserveShortBranches)
        {
            if (assemblyReference == null) return null;
            if (CoreSystemTypes.SystemAssembly == null) Debug.Fail("");
            var reader = new Reader(cache, doNotLockFile, getDebugInfo, useGlobalCache, preserveShortBranches);
            return assemblyReference.Assembly = reader.GetAssemblyFromReference(assemblyReference);
        }
#endif
#if !NoReflection
        public static AssemblyNode GetAssembly(Assembly runtimeAssembly)
        {
            return GetAssembly(runtimeAssembly, null, false, true, false);
        }

        public static AssemblyNode GetAssembly(Assembly runtimeAssembly, IDictionary cache)
        {
            return GetAssembly(runtimeAssembly, cache, false, false, false);
        }

        public static AssemblyNode GetAssembly(Assembly runtimeAssembly, IDictionary cache, bool getDebugInfo,
            bool useGlobalCache)
        {
            return GetAssembly(runtimeAssembly, cache, getDebugInfo, useGlobalCache, false);
        }

        public static AssemblyNode GetAssembly(Assembly runtimeAssembly, IDictionary cache, bool getDebugInfo,
            bool useGlobalCache, bool preserveShortBranches)
        {
            if (runtimeAssembly == null) return null;
            if (CoreSystemTypes.SystemAssembly == null) Debug.Fail("");
            if (runtimeAssembly.GetName().Name == "mscorlib") return CoreSystemTypes.SystemAssembly;
            if (CompiledAssemblies != null)
            {
                var weakRef = (WeakReference)CompiledAssemblies[runtimeAssembly];
                if (weakRef != null)
                {
                    var assem = (AssemblyNode)weakRef.Target;
                    if (assem == null) CompiledAssemblies.Remove(runtimeAssembly); //Remove the dead WeakReference
                    return assem;
                }
            }

            if (runtimeAssembly.Location != null && runtimeAssembly.Location.Length > 0)
                return GetAssembly(runtimeAssembly.Location, cache, false, getDebugInfo, useGlobalCache,
                    preserveShortBranches);
            //Get here for in memory assemblies that were not loaded from a known AssemblyNode
            //Need CLR support to handle such assemblies. For now return null.
            return null;
        }
#endif
        public void SetupDebugReader(string pdbSearchPath)
        {
            if (reader == null)
            {
                Debug.Assert(false);
                return;
            }

            reader.SetupDebugReader(Location, pdbSearchPath);
        }

        internal static string /*!*/ GetStrongName(string name, Version version, string culture, byte[] publicKey,
            bool retargetable)
        {
            if (version == null) version = new Version();
            var result = new StringBuilder();
            result.Append(name);
            result.Append(", Version=");
            result.Append(version);
            result.Append(", Culture=");
            result.Append(culture == null || culture.Length == 0 ? "neutral" : culture);
            result.Append(GetKeyString(publicKey));
            if (retargetable)
                result.Append(", Retargetable=Yes");
            return result.ToString();
        }

        private Reflection.AssemblyName assemblyName;
        public Reflection.AssemblyName GetAssemblyName()
        {
            if (assemblyName == null)
            {
                var aName = new Reflection.AssemblyName();
                if (Location != null && Location != "unknown:location")
                {
                    var sb = new StringBuilder("file:///");
                    sb.Append(Path.GetFullPath(Location));
                    sb.Replace('\\', '/');
                    aName.CodeBase = sb.ToString();
                }

                aName.CultureInfo = new CultureInfo(Culture);
                if (PublicKeyOrToken != null && PublicKeyOrToken.Length > 8)
                    aName.Flags = AssemblyNameFlags.PublicKey;
                if ((Flags & AssemblyFlags.Retargetable) != 0)
                    aName.Flags |= (AssemblyNameFlags)AssemblyFlags.Retargetable;
                aName.HashAlgorithm = (Configuration.Assemblies.AssemblyHashAlgorithm)HashAlgorithm;
                if (PublicKeyOrToken != null && PublicKeyOrToken.Length > 0)
                    aName.SetPublicKey(PublicKeyOrToken);
                else
                    aName.SetPublicKey(new byte[0]);
                aName.Name = Name;
                aName.Version = Version;
                switch (Flags & AssemblyFlags.CompatibilityMask)
                {
                    case AssemblyFlags.NonSideBySideCompatible:
                        aName.VersionCompatibility = AssemblyVersionCompatibility.SameDomain;
                        break;
                    case AssemblyFlags.NonSideBySideProcess:
                        aName.VersionCompatibility = AssemblyVersionCompatibility.SameProcess;
                        break;
                    case AssemblyFlags.NonSideBySideMachine:
                        aName.VersionCompatibility = AssemblyVersionCompatibility.SameMachine;
                        break;
                }

                assemblyName = aName;
            }

            return assemblyName;
        }
#if !NoReflection
        private sealed class CachedRuntimeAssembly : IDisposable
        {
            internal Assembly Value;

            internal CachedRuntimeAssembly(Assembly assembly)
            {
                Value = assembly;
            }

            public void Dispose()
            {
                if (Value != null)
                    if (CompiledAssemblies != null)
                        CompiledAssemblies.Remove(Value);
                Value = null;
                GC.SuppressFinalize(this);
            }

            ~CachedRuntimeAssembly()
            {
                Dispose();
            }
        }

        private CachedRuntimeAssembly cachedRuntimeAssembly;
        public Assembly GetRuntimeAssembly()
        {
            return GetRuntimeAssembly(null, null);
        }
#endif
#if !NoReflection
        public Assembly GetRuntimeAssembly(Evidence evidence)
        {
            return GetRuntimeAssembly(evidence, null);
        }

        public Assembly GetRuntimeAssembly(AppDomain targetAppDomain)
        {
            return GetRuntimeAssembly(null, targetAppDomain);
        }

        public Assembly GetRuntimeAssembly(Evidence evidence, AppDomain targetAppDomain)
        {
            var result = cachedRuntimeAssembly == null ? null : cachedRuntimeAssembly.Value;
            if (result == null || evidence != null || targetAppDomain != null)
                lock (this)
                {
                    if (cachedRuntimeAssembly != null && evidence == null && targetAppDomain == null)
                        return cachedRuntimeAssembly.Value;
                    if (targetAppDomain == null) targetAppDomain = AppDomain.CurrentDomain;
                    if (Location != null)
                    {
                        var name = StrongName;
                        var alreadyLoadedAssemblies = targetAppDomain.GetAssemblies();
                        if (alreadyLoadedAssemblies != null)
                            for (int i = 0, n = alreadyLoadedAssemblies.Length; i < n; i++)
                            {
                                var a = alreadyLoadedAssemblies[i];
                                if (a == null) continue;
                                if (a.FullName == name)
                                {
                                    result = a;
                                    break;
                                }
                            }

                        if (result == null)
                        {
                            if (evidence != null)
                                result = targetAppDomain.Load(GetAssemblyName());
                            else
                                result = targetAppDomain.Load(GetAssemblyName());
                        }
                    }
#if !NoWriter
                    // without the writer, it is impossible to get the runtime
                    // assembly for an AssemblyNode which does not correspond
                    // to a file on disk, we will return null in that case.
                    else
                    {
                        byte[] executable = null;
                        byte[] debugSymbols = null;
                        if ((Flags & (AssemblyFlags.EnableJITcompileTracking |
                                      AssemblyFlags.DisableJITcompileOptimizer)) != 0)
                        {
                            WriteModule(out executable, out debugSymbols);
                            if (evidence != null)
                                result = targetAppDomain.Load(executable, debugSymbols);
                            else
                                result = targetAppDomain.Load(executable, debugSymbols);
                        }
                        else
                        {
                            WriteModule(out executable);
                            if (evidence != null)
                                result = targetAppDomain.Load(executable, null);
                            else
                                result = targetAppDomain.Load(executable);
                        }
                    }
#endif
                    if (result != null && evidence == null && targetAppDomain == AppDomain.CurrentDomain)
                    {
                        AddCachedAssembly(result);
                        cachedRuntimeAssembly = new CachedRuntimeAssembly(result);
                    }
                }

            return result;
        }

        private void AddCachedAssembly(Assembly /*!*/ runtimeAssembly)
        {
            if (CompiledAssemblies == null)
                CompiledAssemblies = Hashtable.Synchronized(new Hashtable());
            CompiledAssemblies[runtimeAssembly] = new WeakReference(this);
        }
#endif
        private static string GetKeyString(byte[] publicKey)
        {
            if (publicKey == null) return null;
            var n = publicKey.Length;
            StringBuilder str;
            if (n > 8)
            {
#if !ROTOR
                var sha = new SHA1CryptoServiceProvider();
                publicKey = sha.ComputeHash(publicKey);
                var token = new byte[8];
                for (int i = 0, m = publicKey.Length - 1; i < 8; i++)
                    token[i] = publicKey[m - i];
                publicKey = token;
                n = 8;
#else
        n = 0; //TODO: figure out how to compute the token on ROTOR
#endif
            }

            if (n == 0)
                str = new StringBuilder(", PublicKeyToken=null");
            else
                str = new StringBuilder(", PublicKeyToken=", n * 2 + 17);
            for (var i = 0; i < n; i++)
                str.Append(publicKey[i].ToString("x2"));
            return str.ToString();
        }

        protected TrivialHashtable friends;

        public virtual bool MayAccessInternalTypesOf(AssemblyNode assembly)
        {
            if (this == assembly) return true;
            if (assembly == null || SystemTypes.InternalsVisibleToAttribute == null) return false;
            if (friends == null) friends = new TrivialHashtable();
            var ob = friends[assembly.UniqueKey];
            if (ob == string.Empty) return false;
            if (ob == this) return true;
            var attributes = assembly.Attributes;
            for (int i = 0, n = attributes == null ? 0 : attributes.Count; i < n; i++)
            {
                //^ assert attributes != null;
                var attr = attributes[i];
                if (attr == null) continue;
                var mb = attr.Constructor as MemberBinding;
                if (mb != null)
                {
                    if (mb.BoundMember == null) continue;
                    if (mb.BoundMember.DeclaringType != SystemTypes.InternalsVisibleToAttribute) continue;
                }
                else
                {
                    var lit = attr.Constructor as Literal;
                    if (lit == null) continue;
                    if (lit.Value as TypeNode != SystemTypes.InternalsVisibleToAttribute) continue;
                }

                if (attr.Expressions == null || attr.Expressions.Count < 1) continue;
                var argLit = attr.Expressions[0] as Literal;
                if (argLit == null) continue;
                var friendName = argLit.Value as string;
                if (friendName == null) continue;
                try
                {
                    var ar = new AssemblyReference(friendName);
                    var tok = ar.PublicKeyToken;
                    if (tok != null && PublicKeyOrToken != null) tok = PublicKeyToken;
                    if (!ar.Matches(Name, ar.Version, ar.Culture, tok)) continue;
#if !FxCop
                }
                catch (ArgumentException e)
                {
                    if (MetadataImportErrors == null) MetadataImportErrors = new ArrayList();
                    MetadataImportErrors.Add(e.Message);
                    continue;
                }
#else
        }finally{}
#endif
                friends[assembly.UniqueKey] = this;
                return true;
            }

            friends[assembly.UniqueKey] = string.Empty;
            return false;
        }

        public AssemblyReferenceList GetFriendAssemblies()
        {
            if (SystemTypes.InternalsVisibleToAttribute == null) return null;
            var attributes = Attributes;
            if (attributes == null) return null;
            var n = attributes.Count;
            if (n == 0) return null;
            var result = new AssemblyReferenceList(n);
            for (var i = 0; i < n; i++)
            {
                var attr = attributes[i];
                if (attr == null) continue;
                var mb = attr.Constructor as MemberBinding;
                if (mb != null)
                {
                    if (mb.BoundMember == null) continue;
                    if (mb.BoundMember.DeclaringType != SystemTypes.InternalsVisibleToAttribute) continue;
                }
                else
                {
                    var lit = attr.Constructor as Literal;
                    if (lit == null) continue;
                    if (lit.Value as TypeNode != SystemTypes.InternalsVisibleToAttribute) continue;
                }

                if (attr.Expressions == null || attr.Expressions.Count < 1) continue;
                var argLit = attr.Expressions[0] as Literal;
                if (argLit == null) continue;
                var friendName = argLit.Value as string;
                if (friendName == null) continue;
                result.Add(new AssemblyReference(friendName));
            }

            return result;
        }

        /// <summary>
        ///     The attributes associated with this module. This corresponds to C# custom attributes with the module target
        ///     specifier.
        /// </summary>
        public virtual AttributeList ModuleAttributes
        {
            get
            {
                if (moduleAttributes != null) return moduleAttributes;
                if (provideCustomAttributes != null)
                    lock (GlobalLock)
                    {
                        if (moduleAttributes == null)
                            provideCustomAttributes(this);
                    }
                else
                    moduleAttributes = new AttributeList();

                return moduleAttributes;
            }
            set { moduleAttributes = value; }
        }

        protected AttributeList moduleAttributes;

        protected byte[] token;
        public virtual byte[] PublicKeyToken
        {
            get
            {
                if (this.token != null) return this.token;
                if (PublicKeyOrToken == null || PublicKeyOrToken.Length == 0) return null;
                if (PublicKeyOrToken.Length == 8) return this.token = PublicKeyOrToken;
#if !ROTOR
                var sha = new SHA1CryptoServiceProvider();
                var hashedKey = sha.ComputeHash(PublicKeyOrToken);
                var token = new byte[8];
                for (int i = 0, n = hashedKey.Length - 1; i < 8; i++) token[i] = hashedKey[n - i];
                return this.token = token;
#else
        return null;
#endif
            }
        }
#if !MinimalReader
        public override string ToString()
        {
            return Name;
        }

        public delegate void PostAssemblyLoadProcessor(AssemblyNode loadedAssembly);

        public event PostAssemblyLoadProcessor AfterAssemblyLoad;
        public PostAssemblyLoadProcessor GetAfterAssemblyLoad()
        {
            return AfterAssemblyLoad;
        }
#endif
    }

    public class AssemblyReference : Node
    {
#if !MinimalReader
        public IdentifierList Aliases;
#endif
        protected internal AssemblyNode assembly;
        private Reflection.AssemblyName assemblyName;
        internal Reader Reader;
        protected string strongName;
        private byte[] token;

        public AssemblyReference()
            : base(NodeType.AssemblyReference)
        {
        }

        public AssemblyReference(AssemblyNode /*!*/ assembly)
            : base(NodeType.AssemblyReference)
        {
            Culture = assembly.Culture;
            Flags = assembly.Flags & ~AssemblyFlags.PublicKey;
            HashValue = assembly.HashValue;
            Name = assembly.Name;
            PublicKeyOrToken = assembly.PublicKeyOrToken;
            if (assembly.PublicKeyOrToken != null && assembly.PublicKeyOrToken.Length > 8)
                Flags |= AssemblyFlags.PublicKey;
            Location = assembly.Location;
            Version = assembly.Version;
            this.assembly = assembly;
        }
#if !MinimalReader
        public AssemblyReference(string assemblyStrongName, SourceContext sctx)
            : this(assemblyStrongName)
        {
            SourceContext = sctx;
        }
#endif
        public AssemblyReference(string assemblyStrongName)
            : base(NodeType.AssemblyReference)
        {
            var flags = AssemblyFlags.None;
            if (assemblyStrongName == null)
            {
                Debug.Assert(false);
                assemblyStrongName = "";
            }

            int i = 0, n = assemblyStrongName.Length;
            var name = ParseToken(assemblyStrongName, ref i);
            string version = null;
            string culture = null;
            string token = null;
            string contentType = null;
            while (i < n)
            {
                if (assemblyStrongName[i] != ',')
                    throw new ArgumentException(string.Format(CultureInfo.CurrentCulture,
                        ExceptionStrings.InvalidAssemblyStrongName, assemblyStrongName), "assemblyStrongName");
                i++;
                while (i < n && char.IsWhiteSpace(assemblyStrongName[i])) i++;
                switch (assemblyStrongName[i])
                {
                    case 'v':
                    case 'V':
                        version = ParseAssignment(assemblyStrongName, "Version", ref i);
                        break;
                    case 'c':
                    case 'C':
                        if (PlatformHelpers.StringCompareOrdinalIgnoreCase(assemblyStrongName, i, "Culture", 0,
                                "Culture".Length) == 0)
                            culture = ParseAssignment(assemblyStrongName, "Culture", ref i);
                        else
                            contentType = ParseAssignment(assemblyStrongName, "ContentType", ref i);
                        break;
                    case 'p':
                    case 'P':
                        if (PlatformHelpers.StringCompareOrdinalIgnoreCase(assemblyStrongName, i, "PublicKeyToken", 0,
                                "PublicKeyToken".Length) == 0)
                        {
                            token = ParseAssignment(assemblyStrongName, "PublicKeyToken", ref i);
                        }
                        else
                        {
                            token = ParseAssignment(assemblyStrongName, "PublicKey", ref i);
                            flags |= AssemblyFlags.PublicKey;
                        }

                        break;
                    case 'r':
                    case 'R':
                        var yesOrNo = ParseAssignment(assemblyStrongName, "Retargetable", ref i);
                        if (PlatformHelpers.StringCompareOrdinalIgnoreCase(yesOrNo, "Yes") == 0)
                            flags |= AssemblyFlags.Retargetable;
                        break;
                }

                while (i < n && assemblyStrongName[i] == ']') i++;
            }

            while (i < n && char.IsWhiteSpace(assemblyStrongName[i])) i++;
            if (i < n)
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture,
                    ExceptionStrings.InvalidAssemblyStrongName, assemblyStrongName), "assemblyStrongName");
            if (PlatformHelpers.StringCompareOrdinalIgnoreCase(culture, "neutral") == 0)
                culture = null;
            if (PlatformHelpers.StringCompareOrdinalIgnoreCase(token, "null") == 0)
                token = null;
            byte[] tok = null;
            if (token != null && (n = token.Length) > 0)
            {
                if (n > 16)
                {
                    var tokArr = new ArrayList();
                    if (n % 2 == 1)
                    {
                        tokArr.Add(byte.Parse(token.Substring(0, 1), NumberStyles.HexNumber, null));
                        n--;
                    }

                    for (i = 0; i < n; i += 2)
                    {
#if WHIDBEY
                        byte b = 0;
                        var goodByte = byte.TryParse(token.Substring(i, 2), NumberStyles.HexNumber, null, out b);
                        Debug.Assert(goodByte);
#else
            byte b = byte.Parse(token.Substring(i, 2), System.Globalization.NumberStyles.HexNumber, null);
#endif
                        tokArr.Add(b);
                    }

                    tok = (byte[])tokArr.ToArray(typeof(byte));
                }
                else
                {
                    var tk = ulong.Parse(token, NumberStyles.HexNumber, null);
                    tok = new byte[8];
                    tok[0] = (byte)(tk >> 56);
                    tok[1] = (byte)(tk >> 48);
                    tok[2] = (byte)(tk >> 40);
                    tok[3] = (byte)(tk >> 32);
                    tok[4] = (byte)(tk >> 24);
                    tok[5] = (byte)(tk >> 16);
                    tok[6] = (byte)(tk >> 8);
                    tok[7] = (byte)tk;
                }
            }

            Culture = culture;
            Name = name;
            PublicKeyOrToken = tok;
            Version = version == null || version.Length == 0 ? null : new Version(version);
            Flags = flags;
        }

        public string Culture { get; set; }

        public AssemblyFlags Flags { get; set; }

        public byte[] HashValue { get; set; }

        public string Name { get; set; }

        public byte[] PublicKeyOrToken { get; set; }

        public Version Version { get; set; }

        public string Location { get; set; }

        public virtual AssemblyNode Assembly
        {
            get
            {
                if (assembly != null) return assembly;
                if (Reader != null)
                    return assembly = Reader.GetAssemblyFromReference(this);
                return null;
            }
            set { assembly = value; }
        }

        public virtual string StrongName
        {
            get
            {
                if (strongName == null)
                    strongName = AssemblyNode.GetStrongName(Name, Version, Culture, PublicKeyOrToken,
                        (Flags & AssemblyFlags.Retargetable) != 0);
                return strongName;
            }
        }

        public byte[] PublicKeyToken
        {
            get
            {
                if (this.token != null) return this.token;
                if (PublicKeyOrToken == null || PublicKeyOrToken.Length == 0) return null;
                if (PublicKeyOrToken.Length == 8) return this.token = PublicKeyOrToken;
#if !ROTOR
                SHA1 sha1 = new SHA1CryptoServiceProvider();
                var hashedKey1 = sha1.ComputeHash(PublicKeyOrToken);
                var sha = new SHA1CryptoServiceProvider();
                var hashedKey = sha.ComputeHash(PublicKeyOrToken);
                var token = new byte[8];
                for (int i = 0, n = hashedKey.Length - 1; i < 8; i++) token[i] = hashedKey[n - i];
                return this.token = token;
#else
        return null;
#endif
            }
        }

        private static string ParseToken(string /*!*/ assemblyStrongName, ref int i)
        {
            Contract.Requires(assemblyStrongName != null);

            var n = assemblyStrongName.Length;
            Debug.Assert(0 <= i && i < n);
            while (i < n && char.IsWhiteSpace(assemblyStrongName[i])) i++;
            var sb = new StringBuilder(n);
            while (i < n)
            {
                var ch = assemblyStrongName[i];
                if (ch == ',' || ch == ']' || char.IsWhiteSpace(ch)) break;
                sb.Append(ch);
                i++;
            }

            while (i < n && char.IsWhiteSpace(assemblyStrongName[i])) i++;
            return sb.ToString();
        }

        private static string ParseAssignment(string /*!*/ assemblyStrongName, string /*!*/ target, ref int i)
        {
            Debug.Assert(assemblyStrongName != null && target != null);
            var n = assemblyStrongName.Length;
            Debug.Assert(0 < i && i < n);
            if (PlatformHelpers.StringCompareOrdinalIgnoreCase(assemblyStrongName, i, target, 0, target.Length) != 0)
                goto throwError;
            i += target.Length;
            while (i < n && char.IsWhiteSpace(assemblyStrongName[i])) i++;
            if (i >= n || assemblyStrongName[i] != '=') goto throwError;
            i++;
            if (i >= n) goto throwError;
            return ParseToken(assemblyStrongName, ref i);
            throwError:
            throw new ArgumentException(string.Format(CultureInfo.CurrentCulture,
                ExceptionStrings.InvalidAssemblyStrongName, assemblyStrongName), "assemblyStrongName");
        }

        public Reflection.AssemblyName GetAssemblyName()
        {
            if (assemblyName == null)
            {
                var aName = new Reflection.AssemblyName();
                aName.CultureInfo = new CultureInfo(Culture == null ? "" : Culture);
                if (PublicKeyOrToken != null && PublicKeyOrToken.Length > 8)
                    aName.Flags = AssemblyNameFlags.PublicKey;
                if ((Flags & AssemblyFlags.Retargetable) != 0)
                    aName.Flags |= (AssemblyNameFlags)AssemblyFlags.Retargetable;
                aName.HashAlgorithm = Configuration.Assemblies.AssemblyHashAlgorithm.SHA1;
                if (PublicKeyOrToken != null)
                {
                    if (PublicKeyOrToken.Length > 8)
                        aName.SetPublicKey(PublicKeyOrToken);
                    else if (PublicKeyOrToken.Length > 0)
                        aName.SetPublicKeyToken(PublicKeyOrToken);
                }
                else
                {
                    aName.SetPublicKey(new byte[0]);
                }

                aName.Name = Name;
                aName.Version = Version;
                switch (Flags & AssemblyFlags.CompatibilityMask)
                {
                    case AssemblyFlags.NonSideBySideCompatible:
                        aName.VersionCompatibility = AssemblyVersionCompatibility.SameDomain;
                        break;
                    case AssemblyFlags.NonSideBySideProcess:
                        aName.VersionCompatibility = AssemblyVersionCompatibility.SameProcess;
                        break;
                    case AssemblyFlags.NonSideBySideMachine:
                        aName.VersionCompatibility = AssemblyVersionCompatibility.SameMachine;
                        break;
                }

                assemblyName = aName;
            }

            return assemblyName;
        }

        public bool Matches(string name, Version version, string culture, byte[] publicKeyToken)
        {
            if (culture != null && culture.Length == 0) culture = null;
            if (Culture != null && Culture.Length == 0) Culture = null;
            if (Version != version && Version != null && (version == null || !Version.Equals(version))) return false;
            if (PlatformHelpers.StringCompareOrdinalIgnoreCase(Name, name) != 0 ||
                PlatformHelpers.StringCompareOrdinalIgnoreCase(Culture, culture) != 0) return false;
            if ((Flags & AssemblyFlags.Retargetable) != 0) return true;
            var thisToken = PublicKeyToken;
            if (publicKeyToken == null) return thisToken == null;
            if (thisToken == publicKeyToken) return true;
            if (thisToken == null) return false;
            var n = publicKeyToken.Length;
            if (n != thisToken.Length) return false;
            for (var i = 0; i < n; i++)
                if (thisToken[i] != publicKeyToken[i])
                    return false;
            return true;
        }

        public bool MatchesIgnoringVersion(AssemblyReference reference)
        {
            if (reference == null) return false;
            return Matches(reference.Name, Version, reference.Culture, reference.PublicKeyToken);
        }
    }

    public class ModuleReference : Node
    {
        public ModuleReference()
            : base(NodeType.ModuleReference)
        {
        }

        public ModuleReference(string name, Module module)
            : base(NodeType.ModuleReference)
        {
            Name = name;
            Module = module;
        }

        public Module Module { get; set; }

        public string Name { get; set; }
    }

    /// <summary>
    ///     A member of a Namespace or a TypeNode
    /// </summary>
    public abstract class Member : Node
    {
#if !MinimalReader
        /// <summary>The namespace of which this node is a member. Null if this node is a member of type.</summary>
        public Namespace DeclaringNamespace;

        /// <summary>
        ///     Indicates that the signature of this member may include unsafe types such as pointers. For methods and properties,
        ///     it also indicates that the
        ///     code may contain unsafe constructions such as pointer arithmetic.
        /// </summary>
        public bool IsUnsafe;

        /// <summary>A list of other nodes that refer to this member. Must be filled in by client code.</summary>
        public NodeList References;
#endif
        protected Member(NodeType nodeType)
            : base(nodeType)
        {
        }

        protected Member(TypeNode declaringType, AttributeList attributes, Identifier name, NodeType nodeType)
            : base(nodeType)
        {
            this.attributes = attributes;
            this.declaringType = declaringType;
            Name = name;
        }

        private TypeNode declaringType;

        /// <summary>The type of which this node is a member. Null if this node is a member of a Namespace.</summary>
        public virtual TypeNode DeclaringType
        {
            get { return declaringType; }
            set { declaringType = value; }
        }

        /// <summary>The unqualified name of the member.</summary>
        public Identifier Name { get; set; }
#if ExtendedRuntime
    private Anonymity anonymity;
#endif
        protected AttributeList attributes;
        private bool notObsolete;
        private ObsoleteAttribute obsoleteAttribute;

        /// <summary>
        ///     The attributes of this member. Corresponds to custom attribute annotations in C#.
        /// </summary>
        public virtual AttributeList Attributes
        {
            get
            {
#if CodeContracts
                Contract.Ensures(Contract.Result<AttributeList>() != null);
#endif
                if (attributes != null) return attributes;
                return attributes = new AttributeList();
            }
            set { attributes = value; }
        }

        protected Member hiddenMember;

        public virtual Member HiddenMember
        {
            get { return hiddenMember; }
            set { hiddenMember = value; }
        }

        protected bool hidesBaseClassMemberSpecifiedExplicitly;
        protected bool hidesBaseClassMember;

        /// <summary>
        ///     Indicates if this is a member of a subclass that intentionally has the same signature as a member of a base
        ///     class. Corresponds to the "new" modifier in C#.
        /// </summary>
        public bool HidesBaseClassMember
        {
            get
            {
                if (hidesBaseClassMemberSpecifiedExplicitly)
                    return hidesBaseClassMember;
                return HiddenMember != null;
            }
            set
            {
                hidesBaseClassMember = value;
                hidesBaseClassMemberSpecifiedExplicitly = true;
            }
        }

        protected Member overriddenMember;

        public virtual Member OverriddenMember
        {
            get { return overriddenMember; }
            set { overriddenMember = value; }
        }

        protected bool overridesBaseClassMemberSpecifiedExplicitly;
        protected bool overridesBaseClassMember;

        /// <summary>
        ///     Indicates if this is a virtual method of a subclass that intentionally overrides a method of a base class.
        ///     Corresponds to the "override" modifier in C#.
        /// </summary>
        public virtual bool OverridesBaseClassMember
        {
            get
            {
                if (overridesBaseClassMemberSpecifiedExplicitly)
                    return overridesBaseClassMember;
                return OverriddenMember != null;
            }
            set
            {
                overridesBaseClassMember = value;
                overridesBaseClassMemberSpecifiedExplicitly = true;
            }
        }

        /// <summary>
        ///     Gets the first attribute of the given type in the attribute list of this member. Returns null if none found.
        ///     This should not be called until the AST containing this member has been processed to replace symbolic references
        ///     to members with references to the actual members.
        /// </summary>
#if ExtendedRuntime || CodeContracts
        [Pure]
#endif
        public virtual AttributeNode GetAttribute(TypeNode attributeType)
        {
            if (attributeType == null) return null;
            var attributes = Attributes;
            for (int i = 0, n = attributes == null ? 0 : attributes.Count; i < n; i++)
            {
                var attr = attributes[i];
                if (attr == null) continue;
                var mb = attr.Constructor as MemberBinding;
                if (mb != null)
                {
                    if (mb.BoundMember == null) continue;
                    if (mb.BoundMember.DeclaringType != attributeType) continue;
                    return attr;
                }

                var lit = attr.Constructor as Literal;
                if (lit == null) continue;
                if (lit.Value as TypeNode != attributeType) continue;
                return attr;
            }

            return null;
        }

        public virtual AttributeList GetFilteredAttributes(TypeNode attributeType)
        {
            if (attributeType == null) return Attributes;
            var attributes = Attributes;
            var filtered = new AttributeList();
            for (int i = 0, n = attributes == null ? 0 : attributes.Count; i < n; i++)
            {
                var attr = attributes[i];
                if (attr == null) continue;
                var mb = attr.Constructor as MemberBinding;
                if (mb != null)
                {
                    if (mb.BoundMember != null && mb.BoundMember.DeclaringType == attributeType) continue;
                    filtered.Add(attr);
                    continue;
                }

                var lit = attr.Constructor as Literal;
                if (lit != null && lit.Value as TypeNode == attributeType) continue;
                filtered.Add(attr);
            }

            return filtered;
        }
#if ExtendedRuntime
    public virtual AttributeList GetAllAttributes(TypeNode attributeType)
    {
      AttributeList filtered = new AttributeList();
      if (attributeType == null) return filtered;
      AttributeList attributes = this.Attributes;
      for (int i = 0, n = attributes == null ? 0 : attributes.Count; i < n; i++) {
        AttributeNode attr = attributes[i];
        if (attr == null) continue;
        MemberBinding mb = attr.Constructor as MemberBinding;
        if (mb != null) {
          if (mb.BoundMember != null && mb.BoundMember.DeclaringType == attributeType)
            filtered.Add(attr);
          continue;
        }
        Literal lit = attr.Constructor as Literal;
        if (lit != null && (lit.Value as TypeNode) == attributeType)
          filtered.Add(attr);
      }
      return filtered;
    }

    /// <summary>
    /// If this is true, the name of the member is meaningless and the member is intended as an "invisible" container for other members.
    /// The value of this property is controlled by the presence or absence of the Anonymous attribute.
    /// </summary>
    public bool IsAnonymous{
      get{ 
        switch (this.Anonymity){
          case Anonymity.None:
          case Anonymity.Unknown:
            return false;
          default:
            return true;
        }
      }
    }
    /// <summary>
    /// Exposes the value of the Anonymous attribute. The value is Anonimity.None if no attribute is present.
    /// </summary>
    public Anonymity Anonymity{
      get{
        if (this.anonymity == Anonymity.Unknown){
          AttributeNode attr = this.GetAttribute(SystemTypes.AnonymousAttribute);
          if (attr == null)
            this.anonymity = Anonymity.None;
          else{
            this.anonymity = Anonymity.Structural; // default
            if (attr.Expressions != null){
              for (int i = 0, n = attr.Expressions.Count; i < n; i++){
                NamedArgument na = attr.Expressions[i] as NamedArgument;
                if (na == null || na.Name != null) continue;
                if (na.Name.UniqueIdKey == StandardIds.Anonymity.UniqueIdKey){
                  Literal lit = na.Value as Literal;
                  if (lit == null) continue;
                  this.anonymity = (Anonymity)lit.Value;
                  break;
                }
              }
            }
          }
        }
        return this.anonymity;
      }
    }
    CciMemberKind cciKind;
    public CciMemberKind CciKind{
      get{
        if (cciKind == CciMemberKind.Unknown){
          AttributeNode a = GetAttribute(SystemTypes.CciMemberKindAttribute);
          if (a == null)
            cciKind = CciMemberKind.Regular;
          else
            cciKind = (CciMemberKind) ((Literal) a.Expressions[0]).Value;
        }
        return cciKind;
      }
      set{
        this.cciKind = value;
      }
    }


#endif
        /// <summary>
        ///     The concatenation of the FullName of the containing member and the name of this member.
        ///     Separated with a '.' character if the containing member is a namespace and a '+' character if the containing member
        ///     is a Type.
        ///     Includes the parameter type full names when this member is a method or a property. Also includes (generic) template
        ///     arguments.
        /// </summary>
        public abstract string /*!*/ FullName { get; }

        /// <summary>True if all references to this member must be from the assembly containing the definition of this member. </summary>
        public abstract bool IsAssembly { get; }

        /// <summary>
        ///     True if access to this member is controlled by the compiler and not the runtime. Cannot be accessed from other
        ///     assemblies since these
        ///     are not necessarily controlled by the same compiler.
        /// </summary>
        public abstract bool IsCompilerControlled { get; }

        /// <summary>True if this member can only be accessed from subclasses of the class declaring this member.</summary>
        public abstract bool IsFamily { get; }

        /// <summary>
        ///     True if this member can only be accessed from subclasses of the class declaring this member, provided that these
        ///     subclasses are also
        ///     contained in the assembly containing this member.
        /// </summary>
        public abstract bool IsFamilyAndAssembly { get; }

        /// <summary>
        ///     True if all references to this member must either be from the assembly containing the definition of this member,
        ///     or from a subclass of the class declaring this member.
        /// </summary>
        public abstract bool IsFamilyOrAssembly { get; }

        /// <summary>True if all references to this member must be from members of the type declaring this member./// </summary>
        public abstract bool IsPrivate { get; }

        /// <summary>True if the member can be accessed from anywhere./// </summary>
        public abstract bool IsPublic { get; }

        /// <summary>
        ///     True if the name of this member conforms to a naming pattern with special meaning. For example the name of a
        ///     property getter.
        /// </summary>
        public abstract bool IsSpecialName { get; }

        /// <summary>True if this member always has the same value or behavior for all instances the declaring type.</summary>
        public abstract bool IsStatic { get; }

        /// <summary>True if another assembly can contain a reference to this member.</summary>
        public abstract bool IsVisibleOutsideAssembly { get; }

        /// <summary>A cached reference to the first Obsolete attribute of this member. Null if no such attribute exsits.</summary>
        public ObsoleteAttribute ObsoleteAttribute
        {
            get
            {
                if (notObsolete) return null;
                if (obsoleteAttribute == null)
                {
                    var attr = GetAttribute(SystemTypes.ObsoleteAttribute);
                    if (attr != null)
                    {
                        var args = attr.Expressions;
                        var numArgs = args == null ? 0 : args.Count;
                        var lit0 = numArgs > 0 ? args[0] as Literal : null;
                        var lit1 = numArgs > 1 ? args[1] as Literal : null;
                        var message = lit0 != null ? lit0.Value as string : null;
                        var isError = lit1 != null ? lit1.Value : null;
                        if (isError is bool)
                            return obsoleteAttribute = new ObsoleteAttribute(message, (bool)isError);
                        return obsoleteAttribute = new ObsoleteAttribute(message);
                    }

                    notObsolete = true;
                }

                return obsoleteAttribute;
            }
            set
            {
                obsoleteAttribute = value;
                notObsolete = false;
            }
        }
#if !MinimalReader
        /// <summary>The source code, if any, corresponding to the value in Documentation.</summary>
        public Node DocumentationNode;
#endif
#if !NoXml
        protected XmlNode documentation;

        /// <summary>
        ///     The body of an XML element containing a description of this member. Used to associated documentation (such as this
        ///     comment) with members.
        ///     The fragment usually conforms to the structure defined in the C# standard.
        /// </summary>
        public virtual XmlNode Documentation
        {
            get
            {
                var documentation = this.documentation;
                if (documentation != null) return documentation;
                var t = DeclaringType;
                if (t == null) t = this as TypeNode;
                var m = t == null ? null : t.DeclaringModule;
                var cache = m == null ? null : m.GetMemberDocumentationCache();
                if (cache == null) return null;
                return this.documentation = (XmlNode)cache[DocumentationId.UniqueIdKey];
            }
            set { documentation = value; }
        }

        protected Identifier documentationId;

        protected virtual Identifier GetDocumentationId()
        {
            return Identifier.Empty;
        }

        /// <summary>
        ///     The value of the name attribute of the XML element whose body is the XML fragment returned by Documentation.
        /// </summary>
        public Identifier DocumentationId
        {
            get
            {
                var documentationId = this.documentationId;
                if (documentationId != null) return documentationId;
                return DocumentationId = GetDocumentationId();
            }
            set { documentationId = value; }
        }

        protected string helpText;

        /// <summary>
        ///     The value of the summary child element of the XML fragment returned by Documentation. All markup is stripped from
        ///     the value.
        /// </summary>
        public virtual string HelpText
        {
            get
            {
                var helpText = this.helpText;
                if (helpText != null) return helpText;
                var documentation = Documentation;
                if (documentation != null && documentation.HasChildNodes) //^ assume documentation.ChildNodes != null;
                    foreach (XmlNode child in documentation.ChildNodes)
                        if (child.Name == "summary")
                            return this.helpText = GetHelpText(child);
                return this.helpText = "";
            }
            set { helpText = value; }
        }

        public virtual string GetParameterHelpText(string parameterName)
        {
            var documentation = Documentation;
            if (documentation == null || documentation.ChildNodes == null) return null;
            foreach (XmlNode cdoc in documentation.ChildNodes)
            {
                if (cdoc == null) continue;
                if (cdoc.Name != "param") continue;
                if (cdoc.Attributes == null) continue;
                foreach (XmlAttribute attr in cdoc.Attributes)
                {
                    if (attr == null || attr.Name != "name" || attr.Value != parameterName) continue;
                    if (!cdoc.HasChildNodes) continue;
                    return GetHelpText(cdoc);
                }
            }

            return null;
        }

        private string GetHelpText(XmlNode node)
        {
            if (node == null) return "";
            var sb = new StringBuilder();
            if (node.HasChildNodes)
                foreach (XmlNode child in node.ChildNodes)
                    switch (child.NodeType)
                    {
                        case XmlNodeType.Element:
                            var str = GetHelpText(child);
                            if (str == null || str.Length == 0) continue;
                            if (sb.Length > 0 && !char.IsPunctuation(str[0]))
                                sb.Append(' ');
                            sb.Append(str);
                            break;
                        case XmlNodeType.CDATA:
                        case XmlNodeType.Entity:
                        case XmlNodeType.Text:
                            AppendValue(sb, child);
                            break;
                    }
            else if (node.Attributes != null)
                foreach (XmlAttribute attr in node.Attributes)
                    AppendValue(sb, attr);

            return sb.ToString();
        }

        private int filterPriority;

        public virtual EditorBrowsableState FilterPriority
        {
            get
            {
                if (filterPriority > 0) return (EditorBrowsableState)(filterPriority - 1);
                var prio = 0;
                var documentation = Documentation;
                if (documentation != null && documentation.HasChildNodes)
                    foreach (XmlNode child in documentation.ChildNodes)
                        if (child.Name == "filterpriority")
                        {
                            PlatformHelpers.TryParseInt32(child.InnerText, out prio);
                            switch (prio)
                            {
                                case 2:
                                    filterPriority = (int)EditorBrowsableState.Advanced;
                                    break;
                                case 3:
                                    filterPriority = (int)EditorBrowsableState.Never;
                                    break;
                                default:
                                    filterPriority = (int)EditorBrowsableState.Always;
                                    break;
                            }

                            filterPriority++;
                            return (EditorBrowsableState)(filterPriority - 1);
                        }

                var attributes = Attributes;
                for (int i = 0, n = attributes == null ? 0 : attributes.Count; i < n; i++)
                {
                    //^ assert attributes != null;
                    var attr = attributes[i];
                    if (attr == null || attr.Type == null) continue;
                    if (attr.Expressions == null || attr.Expressions.Count < 1) continue;
                    if (attr.Type.FullName != "System.ComponentModel.EditorBrowsableAttribute") continue;
                    var lit = attr.Expressions[0] as Literal;
                    if (lit == null || !(lit.Value is int)) continue;
                    //^ assert lit.Value != null;
                    prio = (int)lit.Value;
                    return (EditorBrowsableState)((filterPriority = prio + 1) - 1);
                }

                return (EditorBrowsableState)((filterPriority = 1) - 1);
            }
            set { filterPriority = (int)value + 1; }
        }

        /// <summary>
        ///     Writes out an element with tag "element", name attribute DocumentationId.ToString() and body Documentation using
        ///     the provided XmlTextWriter instance.
        /// </summary>
        public virtual void WriteDocumentation(XmlTextWriter xwriter)
        {
            if (documentation == null || xwriter == null) return;
            xwriter.WriteStartElement("member");
            if (DocumentationId == null) return;
            xwriter.WriteAttributeString("name", DocumentationId.ToString());
            documentation.WriteContentTo(xwriter);
            xwriter.WriteEndElement();
        }

        private static readonly char[] /*!*/
            tags = { 'E', 'F', 'M', 'P', 'T' };

        private void AppendValue(StringBuilder /*!*/ sb, XmlNode /*!*/ node)
        {
            var str = node.Value;
            if (str != null)
            {
                str = str.Trim();
                if (str.Length > 2 && str[1] == ':' && str.LastIndexOfAny(tags, 0, 1) == 0)
                {
                    var tag = str[0];
                    str = str.Substring(2);
                    if (tag == 'T' && str.IndexOf(TargetPlatform.GenericTypeNamesMangleChar) >= 0)
                    {
                        Module mod = null;
                        if (DeclaringType != null)
                            mod = DeclaringType.DeclaringModule;
                        else if (this is TypeNode)
                            mod = ((TypeNode)this).DeclaringModule;
                        if (mod != null)
                        {
                            Identifier ns;
                            Identifier tn;
                            var i = str.LastIndexOf('.');
                            if (i < 0 || i >= str.Length)
                            {
                                ns = Identifier.Empty;
                                tn = Identifier.For(str);
                            }
                            else
                            {
                                ns = Identifier.For(str.Substring(0, i));
                                tn = Identifier.For(str.Substring(i + 1));
                            }

                            var t = mod.GetType(ns, tn, true);
                            if (t != null) str = t.GetFullUnmangledNameWithTypeParameters();
                        }
                    }
                }

                if (str == null || str.Length == 0) return;
                var lastCharWasSpace = false;
                if (sb.Length > 0 && !char.IsPunctuation(str[0]) && !char.IsWhiteSpace(str[0]))
                {
                    sb.Append(' ');
                    lastCharWasSpace = true;
                }

                foreach (var ch in str)
                    if (char.IsWhiteSpace(ch))
                    {
                        if (lastCharWasSpace) continue;
                        lastCharWasSpace = true;
                        sb.Append(' ');
                    }
                    else
                    {
                        lastCharWasSpace = false;
                        sb.Append(ch);
                    }

                if (sb.Length > 0 && char.IsWhiteSpace(sb[sb.Length - 1]))
                    sb.Length -= 1;
            }
        }
#endif
#if FxCop
    internal string GetName(MemberFormat options)
    {
      StringBuilder name = new StringBuilder();
      GetName(options, name);
      return name.ToString();
    }
    internal virtual void GetName(MemberFormat options, StringBuilder name)
    {
      if (options.Type.TypeName != TypeNameFormat.None && this.DeclaringType != null)
      {
        this.DeclaringType.GetName(options, name);
        name.Append('.');
      }
      name.Append(this.Name.Name);
    }
#endif

#if CodeContracts
        public void EnsureMangledName()
        {
            var name = Name.Name;
            var t = this as TypeNode;
            if (t != null)
            {
                if (!t.IsGeneric) return;
                var tcount = t.TemplateParameters == null ? 0 : t.TemplateParameters.Count;
                if (tcount == 0) return;
                var lastIndex = name.LastIndexOf(TargetPlatform.GenericTypeNamesMangleChar);
                if (lastIndex > 0) return;
                name = name + TargetPlatform.GenericTypeNamesMangleChar + tcount;
                Name = Identifier.For(name);
            }
        }
#endif
    }
#if !MinimalReader && !CodeContracts
  public class TypeMemberSnippet : Member{
    public IParserFactory ParserFactory;

    public TypeMemberSnippet()
      : base(NodeType.TypeMemberSnippet){
    }
    public TypeMemberSnippet(IParserFactory parserFactory, SourceContext sctx)
      : base(NodeType.TypeMemberSnippet){
      this.ParserFactory = parserFactory;
      this.SourceContext = sctx;
    }
    public override string/*!*/ FullName {
      get{throw new InvalidOperationException();}
    }
    public override bool IsCompilerControlled{
      get{throw new InvalidOperationException();}
    }
    public override bool IsAssembly{
      get{throw new InvalidOperationException();}
    }
    public override bool IsFamily{
      get{throw new InvalidOperationException();}
    }
    public override bool IsFamilyAndAssembly{
      get{throw new InvalidOperationException();}
    }
    public override bool IsFamilyOrAssembly{
      get{throw new InvalidOperationException();}
    }
    public override bool IsPrivate{
      get{throw new InvalidOperationException();}
    }
    public override bool IsPublic{
      get{throw new InvalidOperationException();}
    }
    public override bool IsSpecialName{
      get{throw new InvalidOperationException();}
    }
    public override bool IsStatic{
      get{throw new InvalidOperationException();}
    }
    public override bool IsVisibleOutsideAssembly {
      get{throw new InvalidOperationException();}
    }

  }
#endif
    /// <summary>
    ///     The common base class for all types. This type should not be extended directly.
    ///     Instead extend one of the standard subclasses such as Class, Struct or Interface, since in
    ///     the CLR a type has to be an instance of one the subclasses, and a type which does not extend
    ///     one of these types will have no equivalent in the CLR.
    /// </summary>
    public abstract class TypeNode : Member
#if CodeContracts
        , IEquatable<TypeNode>
#endif
    {
#if ExtendedRuntime || CodeContracts
        /// <summary>The invariants and modelfield contracts associated with this type (for now only classes, interfaces, structs).</summary>
        public TypeContract Contract
        {
            get
            {
                // delayed by Member provider
                var _ = Members;
                return contract;
            }
            set { contract = value; }
        }

        private TypeContract contract;
#endif
        /// <summary>Specifies the total size in bytes of instances of types with prescribed layout.</summary>
        public int ClassSize { get; set; }

        /// <summary>The module or assembly to which the compiled type belongs.</summary>
        public Module DeclaringModule { get; set; }

        /// <summary>
        ///     For TypeNode, DeclaringType is delay loaded based on the SignatureProvider. Use Interfaces to trigger
        /// </summary>
        public override TypeNode DeclaringType
        {
            get
            {
                var _ = Interfaces;
                return base.DeclaringType;
            }
            set { base.DeclaringType = value; }
        }

        public TypeFlags Flags { get; set; }

        /// <summary>The interfaces implemented by this class or struct, or the extended by this interface.</summary>
        public virtual InterfaceList Interfaces
        {
            get
            {
                if (interfaces == null)
                {
                    var provideTypeSignature = ProvideTypeSignature;
                    if (provideTypeSignature != null && ProviderHandle != null)
                        lock (Module.GlobalLock)
                        {
                            if (interfaces == null)
                            {
                                ProvideTypeSignature = null; // guard against recursion
                                provideTypeSignature(this, ProviderHandle);
                            }
                        }
                    else
                        interfaces = new InterfaceList(0);
                }

                return interfaces;
            }
            set { interfaces = value; }
        }

        protected InterfaceList interfaces;
#if !MinimalReader
        public InterfaceList InterfaceExpressions;
#endif
        /// <summary>The namespace to which this type belongs. Null if the type is nested inside another type.</summary>
        public Identifier Namespace { get; set; }

        /// <summary>Specifies the alignment of fields within types with prescribed layout.</summary>
        public int PackingSize { get; set; }
#if !MinimalReader
        /// <summary>
        ///     If this type is the combined result of a number of partial type definitions, this lists the partial
        ///     definitions.
        /// </summary>
        public TypeNodeList IsDefinedBy;
#endif
        /// <summary>
        ///     True if this type is the result of a template instantiation with arguments that are themselves template parameters.
        ///     Used to model template instantiations occurring inside templates.
        /// </summary>
        public bool IsNotFullySpecialized;

        public bool NewTemplateInstanceIsRecursive;
#if !MinimalReader
        /// <summary>
        ///     If this type is a partial definition, the value of this is the combined type resulting from all the partial
        ///     definitions.
        /// </summary>
        public TypeNode PartiallyDefines;

        /// <summary>
        ///     The list of extensions of this type, if it's a non-extension type.
        ///     all extensions implement the IExtendTypeNode interface (in the Sing# code base).
        ///     null = empty list
        /// </summary>
        private TypeNodeList extensions;

        /// <summary>
        ///     Whether or not the list of extensions has been examined;
        ///     it's a bug to record a new extension after extensions have been examined.
        /// </summary>
        private bool extensionsExamined;

        /// <summary>
        ///     Record another extension of this type.
        /// </summary>
        /// <param name="extension"></param>
        public void RecordExtension(TypeNode extension)
        {
            Debug.Assert(!extensionsExamined, "adding an extension after they've already been examined");
            if (extensions == null) extensions = new TypeNodeList();
            extensions.Add(extension);
        }

        /// <summary>
        ///     The property that should be accessed by clients to get the list of extensions of this type.
        /// </summary>
        public TypeNodeList Extensions
        {
            get
            {
                extensionsExamined = true;
                return extensions;
            }
            set
            {
                Debug.Assert(!extensionsExamined, "setting extensions after they've already been examined");
                extensions = value;
            }
        }

        /// <summary>
        ///     When duplicating a type node, we want to transfer the extensions and the extensionsExamined flag without
        ///     treating this as a "touch" that sets the examined flag.  Pretty ugly, though.
        /// </summary>
        public TypeNodeList ExtensionsNoTouch => extensions;

        /// <summary>
        ///     Copy a (possibly transformed) set of extensions from source to the
        ///     receiver, including whether or not the extensions have been examined.
        /// </summary>
        public void DuplicateExtensions(TypeNode source, TypeNodeList newExtensions)
        {
            if (source == null) return;
            extensions = newExtensions;
            extensionsExamined = source.extensionsExamined;
        }

        /// <summary>
        ///     If the receiver is a type extension, return the extendee, otherwise return the receiver.
        ///     [The identity function, except for dialects (e.g. Extensible Sing#) that allow
        ///     extensions and differing views of types]
        /// </summary>
        public virtual TypeNode /*!*/ EffectiveTypeNode => this;

        /// <summary>
        ///     Return whether t1 represents the same type as t2 (or both are null).
        ///     This copes with the cases where t1 and/or t2 may be type views and/or type extensions, as
        ///     in Extensible Sing#.
        /// </summary>
        public static bool operator ==(TypeNode t1, TypeNode t2)
        {
            return
                (object)t1 == null
                    ? (object)t2 == null
                    : (object)t2 != null && t1.EffectiveTypeNode == (object)t2.EffectiveTypeNode;
        }

        // modify the other operations related to equality
        public static bool operator !=(TypeNode t1, TypeNode t2)
        {
            return !(t1 == t2);
        }

        public override bool Equals(object other)
        {
            return this == other as TypeNode;
        }
#if CodeContracts
        public bool Equals(TypeNode other)
        {
            return this == other;
        }
#endif
        public override int GetHashCode()
        {
            var tn = EffectiveTypeNode;
            if (tn == (object)this)
                return base.GetHashCode();
            return tn.GetHashCode();
        }
#endif
        /// <summary>
        ///     A delegate that is called the first time Members is accessed, if non-null.
        ///     Provides for incremental construction of the type node.
        ///     Must not leave Members null.
        /// </summary>
        public TypeMemberProvider ProvideTypeMembers;

        /// <summary>
        ///     The type of delegates that fill in the Members property of the given type.
        /// </summary>
        public delegate void TypeMemberProvider(TypeNode /*!*/ type, object /*!*/ handle);

        /// <summary>
        ///     A delegate that is called the first time NestedTypes is accessed, if non-null.
        /// </summary>
        public NestedTypeProvider ProvideNestedTypes;

        /// <summary>
        ///     The type of delegates that fill in the NestedTypes property of the given type.
        /// </summary>
        public delegate void NestedTypeProvider(TypeNode /*!*/ type, object /*!*/ handle);

        /// <summary>
        ///     A delegate that is called the first time Attributes is accessed, if non-null.
        ///     Provides for incremental construction of the type node.
        ///     Must not leave Attributes null.
        /// </summary>
        public TypeAttributeProvider ProvideTypeAttributes;

        /// <summary>
        ///     The type of delegates that fill in the Attributes property of the given type.
        /// </summary>
        public delegate void TypeAttributeProvider(TypeNode /*!*/ type, object /*!*/ handle);

        /// <summary>
        ///     A delegate that is called the first time BaseClass, Interfaces, or TemplateParameters,
        ///     or DeclaringType are accessed, if non-null.
        ///     Provides for incremental construction of the type node.
        /// </summary>
        public TypeSignatureProvider ProvideTypeSignature;

        /// <summary>
        ///     The type of delegates that fill in the BaseClass,Interfaces, and TemplateParameters properties of the given type.
        /// </summary>
        public delegate void TypeSignatureProvider(TypeNode /*!*/ type, object /*!*/ handle);

        /// <summary>
        ///     Opaque information passed as a parameter to the delegates in ProvideTypeMembers et al.
        ///     Typically used to associate this namespace instance with a helper object.
        /// </summary>
        public object ProviderHandle;

        internal TypeNode(NodeType nodeType)
            : base(nodeType)
        {
#if ExtendedRuntime
      this.Contract = new TypeContract(this, true);
#endif
        }

        internal TypeNode(NodeType nodeType, NestedTypeProvider provideNestedTypes,
            TypeAttributeProvider provideAttributes, TypeMemberProvider provideMembers, object handle)
            : base(nodeType)
        {
            ProvideNestedTypes = provideNestedTypes;
            ProvideTypeAttributes = provideAttributes;
            ProvideTypeMembers = provideMembers;
            ProviderHandle = handle;
#if !MinimalReader
            isNormalized = true;
#endif
#if ExtendedRuntime
      this.Contract = new TypeContract(this);
#endif
        }

        internal TypeNode(Module declaringModule, TypeNode declaringType, AttributeList attributes, TypeFlags flags,
            Identifier Namespace, Identifier name, InterfaceList interfaces, MemberList members, NodeType nodeType)
            : base(null, attributes, name, nodeType)
        {
            DeclaringModule = declaringModule;
            DeclaringType = declaringType;
            Flags = flags;
            Interfaces = interfaces;
            this.members = members;
            this.Namespace = Namespace;
#if ExtendedRuntime
      this.Contract = new TypeContract(this, true);
#endif
        }

        public override AttributeList Attributes
        {
            get
            {
                if (attributes == null)
                {
                    var provideTypeAttributes = ProvideTypeAttributes;
                    if (provideTypeAttributes != null && ProviderHandle != null)
                        lock (Module.GlobalLock)
                        {
                            if (attributes == null)
                            {
                                ProvideTypeAttributes = null; // guard against recursion/reuse
                                provideTypeAttributes(this, ProviderHandle);
                            }
                        }
                    else
                        attributes = new AttributeList(0);
                }

                return attributes;
            }
            set { attributes = value; }
        }

        protected SecurityAttributeList securityAttributes;

        /// <summary>Contains declarative security information associated with the type.</summary>
        public SecurityAttributeList SecurityAttributes
        {
            get
            {
                if (securityAttributes != null) return securityAttributes;
                if (attributes == null)
                {
                    var
                        al = Attributes; //Getting the type attributes also gets the security attributes, in the case of a type that was read in by the Reader
                    if (al != null) al = null;
                    if (securityAttributes != null) return securityAttributes;
                }

                return securityAttributes = new SecurityAttributeList(0);
            }
            set { securityAttributes = value; }
        }

        /// <summary>The type from which this type is derived. Null in the case of interfaces and System.Object.</summary>
        public virtual TypeNode BaseType
        {
            get
            {
                switch (NodeType)
                {
                    case NodeType.ArrayType: return CoreSystemTypes.Array;
                    case NodeType.ClassParameter:
                    case NodeType.Class: return ((Class)this).BaseClass;
                    case NodeType.DelegateNode: return CoreSystemTypes.MulticastDelegate;
                    case NodeType.EnumNode: return CoreSystemTypes.Enum;
                    case NodeType.Struct:
#if !MinimalReader
                    case NodeType.TupleType:
                    case NodeType.TypeAlias:
                    case NodeType.TypeIntersection:
                    case NodeType.TypeUnion:
#endif
                        return CoreSystemTypes.ValueType;
                    default: return null;
                }
            }
        }

        protected internal MemberList defaultMembers;

        /// <summary>A list of any members of this type that have the DefaultMember attribute.</summary>
        public virtual MemberList DefaultMembers
        {
            get
            {
                var n = Members.Count;
                if (n != memberCount)
                {
                    UpdateMemberTable(n);
                    defaultMembers = null;
                }

                if (defaultMembers == null)
                {
                    var attrs = Attributes;
                    Identifier defMemName = null;
                    for (int j = 0, m = attrs == null ? 0 : attrs.Count; j < m; j++)
                    {
                        //^ assert attrs != null;
                        var attr = attrs[j];
                        if (attr == null) continue;
                        var mb = attr.Constructor as MemberBinding;
                        if (mb != null && mb.BoundMember != null &&
                            mb.BoundMember.DeclaringType == SystemTypes.DefaultMemberAttribute)
                        {
                            if (attr.Expressions != null && attr.Expressions.Count > 0)
                            {
                                var lit = attr.Expressions[0] as Literal;
                                if (lit != null && lit.Value is string)
                                    defMemName = Identifier.For((string)lit.Value);
                            }

                            break;
                        }

                        var litc = attr.Constructor as Literal;
                        if (litc != null && litc.Value as TypeNode == SystemTypes.DefaultMemberAttribute)
                        {
                            if (attr.Expressions != null && attr.Expressions.Count > 0)
                            {
                                var lit = attr.Expressions[0] as Literal;
                                if (lit != null && lit.Value is string)
                                    defMemName = Identifier.For((string)lit.Value);
                            }

                            break;
                        }
                    }

                    if (defMemName != null)
                        defaultMembers = GetMembersNamed(defMemName);
                    else
                        defaultMembers = new MemberList(0);
                }

                return defaultMembers;
            }
            set { defaultMembers = value; }
        }

        protected string fullName;
        public override string /*!*/ FullName
        {
            get
            {
                if (fullName != null) return fullName;
                if (DeclaringType != null)
                    return fullName = DeclaringType.FullName + "+" + (Name == null ? "" : Name.ToString());
                if (Namespace != null && Namespace.UniqueIdKey != Identifier.Empty.UniqueIdKey)
                    return fullName = Namespace + "." + (Name == null ? "" : Name.ToString());
                if (Name != null)
                    return fullName = Name.ToString();
                return fullName = "";
            }
        }
#if !MinimalReader
        // the same as FullName, except for dialects like Sing# with type extensions where names of
        // type extensions may get mangled; in that case, this reports the name of the effective type node.
        public virtual string FullNameDuringParsing => FullName;
#endif
        public virtual string GetFullUnmangledNameWithoutTypeParameters()
        {
            if (DeclaringType != null)
                return DeclaringType.GetFullUnmangledNameWithoutTypeParameters() + "+" +
                       GetUnmangledNameWithoutTypeParameters();
            if (Namespace != null && Namespace.UniqueIdKey != Identifier.Empty.UniqueIdKey)
                return Namespace + "." + GetUnmangledNameWithoutTypeParameters();
            return GetUnmangledNameWithoutTypeParameters();
        }

        public virtual string GetFullUnmangledNameWithTypeParameters()
        {
            if (DeclaringType != null)
                return DeclaringType.GetFullUnmangledNameWithTypeParameters() + "+" +
                       GetUnmangledNameWithTypeParameters(true);
            if (Namespace != null && Namespace.UniqueIdKey != Identifier.Empty.UniqueIdKey)
                return Namespace + "." + GetUnmangledNameWithTypeParameters(true);
            return GetUnmangledNameWithTypeParameters(true);
        }

        public virtual string GetUnmangledNameWithTypeParameters()
        {
            return GetUnmangledNameWithTypeParameters(false);
        }

        private string GetUnmangledNameWithTypeParameters(bool fullNamesForTypeParameters)
        {
            var sb = new StringBuilder(GetUnmangledNameWithoutTypeParameters());
            var templateParameters = TemplateParameters;
            if (Template != null) templateParameters = TemplateArguments;
            for (int i = 0, n = templateParameters == null ? 0 : templateParameters.Count; i < n; i++)
            {
                //^ assert templateParameters != null;
                var tpar = templateParameters[i];
                if (tpar == null) continue;
                if (i == 0)
                    sb.Append('<');
                else
                    sb.Append(',');
                if (tpar.Name != null)
                    if (fullNamesForTypeParameters)
                        sb.Append(tpar.GetFullUnmangledNameWithTypeParameters());
                    else
                        sb.Append(tpar.GetUnmangledNameWithTypeParameters());
                if (i == n - 1)
                    sb.Append('>');
            }

            return sb.ToString();
        }

        protected static readonly char[] /*!*/
            MangleChars = { '!', '>' };

        public virtual string /*!*/ GetUnmangledNameWithoutTypeParameters()
        {
            MangleChars[0] = TargetPlatform.GenericTypeNamesMangleChar;
            if (Template != null) return Template.GetUnmangledNameWithoutTypeParameters();
            if (Name == null) return "";
            var name = Name.ToString();
            if (TemplateParameters != null && TemplateParameters.Count > 0)
            {
                var lastMangle = name.LastIndexOfAny(MangleChars);
                if (lastMangle >= 0)
                {
                    if (name[lastMangle] == '>') lastMangle++;
                    return name.Substring(0, lastMangle);
                }
            }

            return name;
        }

#if !MinimalReader
        public virtual string GetSerializedTypeName()
        {
            var isAssemblyQualified = true;
            return GetSerializedTypeName(this, ref isAssemblyQualified);
        }

        private string GetSerializedTypeName(TypeNode /*!*/ type, ref bool isAssemblyQualified)
        {
            if (type == null) return null;
            var sb = new StringBuilder();
            var tMod = type as TypeModifier;
            if (tMod != null)
                type = tMod.ModifiedType;
            var arrType = type as ArrayType;
            if (arrType != null)
            {
                type = arrType.ElementType;
                var isAssemQual = false;
                AppendSerializedTypeName(sb, arrType.ElementType, ref isAssemQual);
                if (arrType.IsSzArray())
                {
                    sb.Append("[]");
                }
                else
                {
                    sb.Append('[');
                    if (arrType.Rank == 1) sb.Append('*');
                    for (var i = 1; i < arrType.Rank; i++) sb.Append(',');
                    sb.Append(']');
                }

                goto done;
            }

            var pointer = type as Pointer;
            if (pointer != null)
            {
                type = pointer.ElementType;
                var isAssemQual = false;
                AppendSerializedTypeName(sb, pointer.ElementType, ref isAssemQual);
                sb.Append('*');
                goto done;
            }

            var reference = type as Reference;
            if (reference != null)
            {
                type = reference.ElementType;
                var isAssemQual = false;
                AppendSerializedTypeName(sb, reference.ElementType, ref isAssemQual);
                sb.Append('&');
                goto done;
            }

            if (type.Template == null)
            {
                sb.Append(type.FullName);
            }
            else
            {
                sb.Append(type.Template.FullName);
                sb.Append('[');
                for (int i = 0, n = type.TemplateArguments == null ? 0 : type.TemplateArguments.Count; i < n; i++)
                {
                    //^ assert type.TemplateArguments != null;
                    var isAssemQual = true;
                    AppendSerializedTypeName(sb, type.TemplateArguments[i], ref isAssemQual);
                    if (i < n - 1) sb.Append(',');
                }

                sb.Append(']');
            }

            done:
            if (isAssemblyQualified)
                AppendAssemblyQualifierIfNecessary(sb, type, out isAssemblyQualified);
            return sb.ToString();
        }

        private void AppendAssemblyQualifierIfNecessary(StringBuilder /*!*/ sb, TypeNode type,
            out bool isAssemQualified)
        {
            isAssemQualified = false;
            if (type == null) return;
            var referencedAssembly = type.DeclaringModule as AssemblyNode;
            if (referencedAssembly != null)
            {
                sb.Append(", ");
                sb.Append(referencedAssembly.StrongName);
                isAssemQualified = true;
            }
        }

        private void AppendSerializedTypeName(StringBuilder /*!*/ sb, TypeNode type, ref bool isAssemQualified)
        {
            if (type == null) return;
            var argTypeName = GetSerializedTypeName(type, ref isAssemQualified);
            if (isAssemQualified) sb.Append('[');
            sb.Append(argTypeName);
            if (isAssemQualified) sb.Append(']');
        }
#endif

        /// <summary>
        ///     Return the name the constructor should have in this type node.  By default, it's
        ///     the same as the name of the enclosing type node, but it can be different in e.g.
        ///     extensions in Extensible Sing#
        /// </summary>
        public virtual Identifier ConstructorName
        {
            get
            {
                if (constructorName == null)
                {
                    var id = Name;
                    if (IsNormalized && IsGeneric)
                        id = Identifier.For(GetUnmangledNameWithoutTypeParameters());
                    constructorName = id;
                }

                return constructorName;
            }
        }

        private Identifier constructorName;


        /// <summary>True if the type is an abstract class or an interface.</summary>
        public virtual bool IsAbstract => (Flags & TypeFlags.Abstract) != 0;

        public override bool IsAssembly
        {
            get
            {
                var visibility = Flags & TypeFlags.VisibilityMask;
                return visibility == TypeFlags.NotPublic || visibility == TypeFlags.NestedAssembly;
            }
        }

        public override bool IsCompilerControlled => false;

        public override bool IsFamily => (Flags & TypeFlags.VisibilityMask) == TypeFlags.NestedFamily;

        public override bool IsFamilyAndAssembly => (Flags & TypeFlags.VisibilityMask) == TypeFlags.NestedFamANDAssem;
        public override bool IsFamilyOrAssembly => (Flags & TypeFlags.VisibilityMask) == TypeFlags.NestedFamORAssem;
        protected bool isGeneric;
        /// <summary>True if this type is a template conforming to the rules of a generic type in the CLR.</summary>
        public virtual bool IsGeneric
        {
            get { return isGeneric; }
            set { isGeneric = value; }
        }
#if ExtendedRuntime
    public static bool IsImmutable(TypeNode type) {
      type = TypeNode.StripModifiers(type);
      if (type == null) return false;
      if (type.TypeCode != TypeCode.Object) return true;
      if (type.GetAttribute(SystemTypes.ImmutableAttribute) != null) return true;
      if (type.IsValueType && type.DeclaringModule == CoreSystemTypes.SystemAssembly) return true; //hack.
      return false;
    }
#endif
        public virtual bool IsNestedAssembly => (Flags & TypeFlags.VisibilityMask) == TypeFlags.NestedAssembly;
        public virtual bool IsNestedFamily => (Flags & TypeFlags.VisibilityMask) == TypeFlags.NestedFamily;

        public virtual bool IsNestedFamilyAndAssembly =>
            (Flags & TypeFlags.VisibilityMask) == TypeFlags.NestedFamANDAssem;

        public virtual bool IsNestedInternal => (Flags & TypeFlags.VisibilityMask) == TypeFlags.NestedFamORAssem;

        public virtual bool IsNestedIn(TypeNode type)
        {
            for (var decType = DeclaringType; decType != null; decType = decType.DeclaringType)
                if (decType == type)
                    return true;
            return false;
        }

        public virtual bool IsNestedPublic => (Flags & TypeFlags.VisibilityMask) == TypeFlags.NestedPublic;
        public virtual bool IsNonPublic => (Flags & TypeFlags.VisibilityMask) == TypeFlags.NotPublic;
#if !MinimalReader
        protected bool isNormalized;
        /// <summary>
        ///     True if the type node is in "normal" form. A node is in "normal" form if it is effectively a node in an AST formed
        ///     directly
        ///     from CLR module or assembly. Such a node can be written out as compiled code to an assembly or module without
        ///     further processing.
        /// </summary>
        public virtual bool IsNormalized
        {
            get
            {
                if (isNormalized) return true;
                if (DeclaringModule == null) return false;
                return isNormalized = DeclaringModule.IsNormalized;
            }
            set { isNormalized = value; }
        }
#endif
        public override bool IsPrivate => (Flags & TypeFlags.VisibilityMask) == TypeFlags.NestedPrivate;

        /// <summary>True if values of this type can be compared directly in CLR IL instructions.</summary>
        public virtual bool IsPrimitiveComparable
        {
            get
            {
                switch (typeCode)
                {
                    case ElementType.Boolean:
                    case ElementType.Char:
                    case ElementType.Int8:
                    case ElementType.Int16:
                    case ElementType.Int32:
                    case ElementType.Int64:
                    case ElementType.IntPtr:
                    case ElementType.UInt8:
                    case ElementType.UInt16:
                    case ElementType.UInt32:
                    case ElementType.UInt64:
                    case ElementType.UIntPtr:
                    case ElementType.Single:
                    case ElementType.Double:
                        return true;
                    default:
                        return !(this is Struct) || this is EnumNode || this is Pointer;
                }
            }
        }

        /// <summary>True if values of this type are integers that can be processed by CLR IL instructions.</summary>
        public virtual bool IsPrimitiveInteger
        {
            get
            {
                switch (typeCode)
                {
                    case ElementType.Int8:
                    case ElementType.Int16:
                    case ElementType.Int32:
                    case ElementType.Int64:
                    case ElementType.IntPtr:
                    case ElementType.UInt8:
                    case ElementType.UInt16:
                    case ElementType.UInt32:
                    case ElementType.UInt64:
                    case ElementType.UIntPtr:
                        return true;
                    default:
                        return false;
                }
            }
        }

        /// <summary>
        ///     True if values of this type are integers or floating point numbers that can be processed by CLR IL
        ///     instructions.
        /// </summary>
        public virtual bool IsPrimitiveNumeric
        {
            get
            {
                switch (typeCode)
                {
                    case ElementType.Int8:
                    case ElementType.Int16:
                    case ElementType.Int32:
                    case ElementType.Int64:
                    case ElementType.IntPtr:
                    case ElementType.UInt8:
                    case ElementType.UInt16:
                    case ElementType.UInt32:
                    case ElementType.UInt64:
                    case ElementType.UIntPtr:
                    case ElementType.Single:
                    case ElementType.Double:
                        return true;
                    default:
                        return false;
                }
            }
        }

        /// <summary>True if values of this type are unsigned integers that can be processed by CLR IL instructions.</summary>
        public virtual bool IsPrimitiveUnsignedInteger
        {
            get
            {
                switch (typeCode)
                {
                    case ElementType.UInt8:
                    case ElementType.UInt16:
                    case ElementType.UInt32:
                    case ElementType.UInt64:
                    case ElementType.UIntPtr:
                        return true;
                    default:
                        return false;
                }
            }
        }

        public override bool IsPublic
        {
            get
            {
                var visibility = Flags & TypeFlags.VisibilityMask;
                return visibility == TypeFlags.Public || visibility == TypeFlags.NestedPublic;
            }
        }

        /// <summary>True if values of this type can be processed by CLR IL instructions.</summary>
        public virtual bool IsPrimitive
        {
            get
            {
                switch (typeCode)
                {
                    case ElementType.Boolean:
                    case ElementType.Char:
                    case ElementType.Double:
                    case ElementType.Int16:
                    case ElementType.Int32:
                    case ElementType.Int64:
                    case ElementType.Int8:
                    case ElementType.IntPtr:
                    case ElementType.Single:
                    case ElementType.String:
                    case ElementType.UInt16:
                    case ElementType.UInt32:
                    case ElementType.UInt64:
                    case ElementType.UInt8:
                    case ElementType.UIntPtr:
                        return true;
                    default:
                        return false;
                }
            }
        }

        /// <summary>True if the type cannot be derived from.</summary>
        public virtual bool IsSealed => (Flags & TypeFlags.Sealed) != 0;

        public override bool IsSpecialName => (Flags & TypeFlags.SpecialName) != 0;
        public override bool IsStatic => true;

        /// <summary>
        ///     True if the identity of the type depends on its structure rather than its name.
        ///     Arrays, pointers and generic type instances are examples of such types.
        /// </summary>
        public virtual bool IsStructural => Template != null;

        /// <summary>True if the type serves as a parameter to a type template.</summary>
        public virtual bool IsTemplateParameter => false;

        /// <summary>True if the type is a value type containing only fields of unmanaged types.</summary>
        public virtual bool IsUnmanaged
        {
            get
            {
#if ExtendedRuntime
        return IsPointerFree;
#else
                return false;
#endif
            }
        }

        /// <summary>A list of the types that contribute to the structure of a structural type.</summary>
        public virtual TypeNodeList StructuralElementTypes
        {
            get
            {
                var result = TemplateArguments;
                if (result != null && result.Count > 0) return result;
                return TemplateParameters;
            }
        }

        /// <summary>True if values of this type are unsigned integers that can be processed by CLR IL instructions.</summary>
        public virtual bool IsUnsignedPrimitiveNumeric
        {
            get
            {
                switch (typeCode)
                {
                    case ElementType.UInt8:
                    case ElementType.UInt16:
                    case ElementType.UInt32:
                    case ElementType.UInt64:
                    case ElementType.UIntPtr:
                        return true;
                    default:
                        return false;
                }
            }
        }

        /// <summary>True if instances of this type have no identity other than their value and are copied upon assignment.</summary>
        public virtual bool IsValueType
        {
            get
            {
                switch (NodeType)
                {
                    case NodeType.EnumNode:
#if !MinimalReader
                    case NodeType.ConstrainedType:
                    case NodeType.TupleType:
                    case NodeType.TypeAlias:
                    case NodeType.TypeIntersection:
                    case NodeType.TypeUnion: return true;
#endif
                    case NodeType.Struct: return true;
                    default: return false;
                }
            }
        }
#if ExtendedRuntime
    /// <summary>True if the type is a value type containing no managed or unmanaged pointers.</summary>
    public virtual bool IsPointerFree
    {
      get
      {
        return false;
      }
    }
#endif
        /// <summary>
        ///     Returns true if the type is definitely a reference type.
        /// </summary>
        public virtual bool IsReferenceType
        {
            get
            {
                switch (NodeType)
                {
                    case NodeType.Class:
                    case NodeType.Interface:
                    case NodeType.Pointer:
                    case NodeType.ArrayType:
                    case NodeType.DelegateNode:
                        return this != SystemTypes.ValueType && this != SystemTypes.Enum;
                    default:
                        return false;
                }
            }
        }

        /// <summary>
        ///     True if underlying type (modulo type modifiers) is a pointer type (Pointer)
        /// </summary>
        public virtual bool IsPointerType => false;

        public override bool IsVisibleOutsideAssembly
        {
            get
            {
                if (DeclaringType != null && !DeclaringType.IsVisibleOutsideAssembly) return false;
                switch (Flags & TypeFlags.VisibilityMask)
                {
                    case TypeFlags.Public:
                    case TypeFlags.NestedPublic:
                        return true;
                    case TypeFlags.NestedFamily:
                    case TypeFlags.NestedFamORAssem:
                        return DeclaringType != null && !DeclaringType.IsSealed;
                    default:
                        return false;
                }
            }
        }

        // This field stores those members declared syntactically within
        // this type node.  (Under Extended Sing#, additional members can
        // be logically part of a type node but declared in a separate
        // syntactic type node.)
        protected internal MemberList members;
        protected internal volatile bool membersBeingPopulated;
        /// <summary>
        ///     The list of members contained inside this type, by default ignoring any extensions of this type.
        ///     (Subclasses in the Extensible Sing# dialect override this to include members of visible extensions.)
        ///     If the value of members is null and the value of ProvideTypeMembers is not null, the
        ///     TypeMemberProvider delegate is called to fill in the value of this property.
        /// </summary>
        public virtual MemberList Members
        {
            get
            {
#if CLOUSOT
                CC.Contract.Ensures(CC.Contract.Result<MemberList>() != null);
#endif
                if (members == null || membersBeingPopulated)
                    if (ProvideTypeMembers != null && ProviderHandle != null)
                        lock (Module.GlobalLock)
                        {
                            if (members == null)
                            {
                                membersBeingPopulated = true;
                                ProvideTypeMembers(this, ProviderHandle);
                                membersBeingPopulated = false;
#if ExtendedRuntime
                this.ApplyOutOfBandContracts();
#endif
                            }
                        }
                    else
                        members = new MemberList();

                return members;
            }
            set
            {
                members = value;
                memberCount = 0;
                memberTable = null;
                constructors = null;
                defaultMembers = null;
#if !MinimalReader
                explicitCoercionFromTable = null;
                explicitCoercionMethods = null;
                explicitCoercionToTable = null;
                implicitCoercionFromTable = null;
                implicitCoercionMethods = null;
                implicitCoercionToTable = null;
                opFalse = null;
                opTrue = null;
#endif
            }
        }
#if ExtendedRuntime
  protected internal TypeNode ContractType
  {
    get
    {
      AssemblyNode declaringAssembly = this.DeclaringModule as AssemblyNode;
      if (declaringAssembly == null || declaringAssembly.ContractAssembly == null) return null;
      if (this.DeclaringType == null)
      {
        return declaringAssembly.ContractAssembly.GetType(this.Namespace, this.Name);
      }
      else
      {
        TypeNode parentContractType = this.DeclaringType.ContractType;
        if (parentContractType == null) return null;
        return parentContractType.GetNestedType(this.Name);
      }
    }
  }
  protected internal virtual void ApplyOutOfBandContracts(){
      if (this.members == null) return;
      TypeNode contractType = this.ContractType;
      if (contractType == null) return;

      // Copy the type-level contract attributes over to the shadowed type, namely "this".
      int contractsNamespaceKey = SystemTypes.NonNullType.Namespace.UniqueIdKey;
      foreach (AttributeNode attr in contractType.Attributes) {
        if (attr.Type.Namespace != null && attr.Type.Namespace.UniqueIdKey == contractsNamespaceKey)
          this.Attributes.Add(attr);
      }

      if (this.BaseType != null) { MemberList junk = this.BaseType.Members; if (junk != null) junk = null; }
      Hashtable contractByFullName = new Hashtable();
      MemberList contractMembers = contractType.Members;
      for (int i = 0, n = contractMembers == null ? 0 : contractMembers.Count; i < n; i++){
        //^ assert contractMembers != null;
        Field f = contractMembers[i] as Field;
        if (f != null) {
          contractByFullName[f.FullName] = f;
          continue;
        }
        Method m = contractMembers[i] as Method;
        if (m == null ) continue;
        string methName = this.FullStrippedName(m);
        contractByFullName[methName] = m;
      }
      for (int i = 0, n = members.Count; i < n; i++){
        Field codeField = members[i] as Field;
        if (codeField != null) {
          Field contractField = contractByFullName[codeField.FullName] as Field;
          if (contractField != null && contractField.Type != null && contractField.Type != codeField.Type) {
            OptionalModifier optFieldType = contractField.Type as OptionalModifier;
            if (optFieldType != null && codeField.Type != null) {
              codeField.Type = OptionalModifier.For(optFieldType.Modifier, codeField.Type);
              codeField.HasOutOfBandContract = true;
            }
          }
          continue;
        }
        Method codeMethod = members[i] as Method;
        if (codeMethod == null) continue;
        // we include the return type since some conversion operators result
        // in overloaded methods whose signatures differ only in return type
        string methName = this.FullStrippedName(codeMethod);
        Method contractMethod = contractByFullName[methName] as Method;
        if (contractMethod != null) {
          this.CopyContractToMethod(contractMethod, codeMethod);
          if (codeMethod.OverridesBaseClassMember) {
            Method overridden = this.FindNearestOverriddenMethod(contractMethod);
            if (overridden != null)
              this.CopyContractToMethod(overridden, codeMethod);
          }
        } else {
          // Maybe there isn't a shadow method declared in contractType, but
          // there still might be out-of-band contracts on an interface method
          // that the codeMethod implements.
          if (codeMethod.ImplementedInterfaceMethods != null && codeMethod.ImplementedInterfaceMethods.Count > 0) {
            foreach (Method m in codeMethod.ImplementedInterfaceMethods) {
              this.CopyContractToMethod(m, codeMethod);
            }
          } else if (codeMethod.ImplicitlyImplementedInterfaceMethods != null) {
            foreach (Method m in codeMethod.ImplicitlyImplementedInterfaceMethods) {
              this.CopyContractToMethod(m, codeMethod);
            }
          }
        }
      }
    }
    protected virtual string/*!*/ FullStrippedName(Method/*!*/ m) {
      StringBuilder sb = new StringBuilder();
      sb.Append(m.DeclaringType.GetFullUnmangledNameWithTypeParameters());
      sb.Append('.');
      if (m.NodeType == NodeType.InstanceInitializer)
        sb.Append("#ctor");
      else if (m.Name != null)
        sb.Append(m.Name.ToString());
      ParameterList parameters = m.Parameters;
      for (int i = 0, n = parameters == null ? 0 : parameters.Count; i < n; i++){
        Parameter par = parameters[i];
        if (par == null || par.Type == null) continue;
        TypeNode parType = TypeNode.DeepStripModifiers(par.Type);
        Reference rt = parType as Reference;
        if (rt != null && rt.ElementType != null)
          parType = TypeNode.DeepStripModifiers(rt.ElementType).GetReferenceType();
        //^ assert parType != null;
        if (i == 0)
          sb.Append('(');
        else
          sb.Append(',');        
        sb.Append(parType.GetFullUnmangledNameWithTypeParameters());
        if (i == n-1)
          sb.Append(')');
      }
      if (m.ReturnType != null){
        TypeNode retType = TypeNode.DeepStripModifiers(m.ReturnType);
        //^ assert retType != null;
        sb.Append(retType.GetFullUnmangledNameWithTypeParameters());
      }
      return sb.ToString();
    }
    protected virtual void CopyContractToMethod(Method/*!*/ contractMethod, Method/*!*/ codeMethod) {
      codeMethod.HasOutOfBandContract = true;
      if (codeMethod.Contract == null)
        codeMethod.Contract = new MethodContract(codeMethod);
      // setting them to null forces deserialization upon next access to the property
      // NB: This means that out-of-band contracts can be applied *only* to code that
      // does *not* have any contracts since this will wipe them out!!
      codeMethod.Contract.Ensures = null;
      codeMethod.Contract.Modifies = null;
      codeMethod.Contract.Requires = null;

      int contractsNamespaceKey = SystemTypes.NonNullType.Namespace.UniqueIdKey;
      // Copy the method-level contract attributes over to the shadowed method.
      for (int a = 0; a < contractMethod.Attributes.Count; a++){
        AttributeNode attr = contractMethod.Attributes[a];
        if (attr != null && attr.Type != null && attr.Type.Namespace != null &&
          attr.Type.Namespace != null && attr.Type.Namespace.UniqueIdKey == contractsNamespaceKey)
          codeMethod.Attributes.Add(attr);
      }
      // Copy over any return attributes to the shadowed method
      for (int a = 0, n =
 contractMethod.ReturnAttributes == null ? 0 : contractMethod.ReturnAttributes.Count; a < n; a++) {
          AttributeNode attr = contractMethod.ReturnAttributes[a];
          if (attr != null) {
              if (attr.Type.Namespace != null && attr.Type.Namespace.UniqueIdKey == contractsNamespaceKey) {
                  if (codeMethod.ReturnAttributes == null) {
                      codeMethod.ReturnAttributes = new AttributeList();
                  }
                  codeMethod.ReturnAttributes.Add(attr);
              }
          }
      }


      // Copy the parameter-level contract attributes and type over to the shadowed method's parameters.
      ParameterList contractParameters = contractMethod.Parameters;
      ParameterList codeParameters = codeMethod.Parameters;
      if (contractParameters != null && codeParameters != null && contractParameters.Count <= codeParameters.Count) {
        for (int i = 0, n = contractParameters.Count; i < n; i++) {
          Parameter contractParameter = contractParameters[i];
          Parameter codeParameter = codeParameters[i];
          if (contractParameter == null || codeParameter == null) continue;
          for (int a = 0, m =
 contractParameter.Attributes == null ? 0 : contractParameter.Attributes.Count; a < m; a++){
            //^ assert contractParameter.Attributes != null;
            AttributeNode attr = contractParameter.Attributes[a];
            if (attr == null || attr.Type == null) continue;
            if (attr.Type.Namespace != null && attr.Type.Namespace.UniqueIdKey == contractsNamespaceKey){
              if (codeParameter.Attributes == null) codeParameter.Attributes = new AttributeList();
              codeParameter.Attributes.Add(attr);
            }
          }
          if (contractParameter.Type != codeParameter.Type)
            codeParameter.Type = this.CopyModifier(contractParameter.Type, codeParameter.Type);
        }
      }
      if (contractMethod.ReturnType != codeMethod.ReturnType)
        codeMethod.ReturnType = this.CopyModifier(contractMethod.ReturnType, codeMethod.ReturnType);
      codeMethod.fullName = null;
    }
    private TypeNode CopyModifier(TypeNode contractType, TypeNode codeType) {
      if (contractType == null) return codeType;
      Reference rcType = contractType as Reference;
      if (rcType != null) {
        contractType = rcType.ElementType;
        if (contractType == null) return codeType;
        Reference rcodeType = codeType as Reference;
        if (rcodeType == null || rcodeType.ElementType == null) return codeType;
        TypeNode t = CopyModifier(contractType, rcodeType.ElementType);
        return t.GetReferenceType();
      }
      ArrayType acType = contractType as ArrayType;
      if (acType != null) {
        contractType = acType.ElementType;
        if (contractType == null) return codeType;
        ArrayType acodeType = codeType as ArrayType;
        if (acodeType == null || acodeType.ElementType == null) return codeType;
        TypeNode t = CopyModifier(contractType, acodeType.ElementType);
        return t.GetArrayType(1);
      }
      OptionalModifier optModType = contractType as OptionalModifier;
      if (optModType != null && optModType.Modifier != null) {
        TypeNode t = CopyModifier(optModType.ModifiedType, codeType);
        codeType = OptionalModifier.For(optModType.Modifier, t);
      }
      if (contractType.Template != null && codeType.Template != null && contractType.TemplateArguments != null && codeType.TemplateArguments != null) {
        TypeNodeList args = contractType.TemplateArguments.Clone();
        TypeNodeList codeArgs = codeType.TemplateArguments;
        for (int i = 0, n = args.Count, m = codeArgs.Count; i < n && i < m; i++) {
          TypeNode argType = args[i];
          TypeNode codeArgType = codeArgs[i];
          if (argType != codeArgType)
            args[i] = this.CopyModifier(argType, codeArgType);
        }
        return codeType.Template.GetTemplateInstance(codeType, args);
      }
      return codeType;
    }
    public virtual Method FindNearestOverriddenMethod (Method method)
      //^ requires method.IsVirtual;
    {
      if (method == null) return null;
      if (!method.IsVirtual) return null;
      int numParams = method.Parameters == null ? 0 : method.Parameters.Count;
      TypeNode[] paramTypes = new TypeNode[numParams];
      for (int i = 0; i<numParams; i++) paramTypes[i] = method.Parameters[i].Type;
      for (TypeNode scan = method.DeclaringType.BaseType; scan != null; scan = scan.BaseType){
        Method overridden = scan.GetMethod(method.Name, paramTypes);
        if (overridden != null) return overridden;
      }
      return null;
    }
    public TypeNodeList ReferencedTemplateInstances;
#endif
        protected TypeNode template;
        /// <summary>
        ///     The (generic) type template from which this type was instantiated. Null if this is not a (generic) type
        ///     template instance.
        /// </summary>
        public virtual TypeNode Template
        {
            get
            {
                var result = template;
                if (result == null)
                {
                    if (isGeneric || TargetPlatform.GenericTypeNamesMangleChar != '_') return null;
                    var attributes = Attributes;
                    lock (this)
                    {
                        if (template != null)
                        {
                            if (template == NotSpecified)
                                return null;
                            return template;
                        }
#if ExtendedRuntime
          for (int i = 0, n = attributes == null ? 0 : attributes.Count; i < n; i++) {
            AttributeNode attr = attributes[i];
            if (attr == null) continue;
            MemberBinding mb = attr.Constructor as MemberBinding;
            if (mb == null || mb.BoundMember == null || mb.BoundMember.DeclaringType != SystemTypes.TemplateInstanceAttribute) continue;
            ExpressionList exprs = attr.Expressions;
            if (exprs == null || exprs.Count != 2) continue;
            Literal lit = exprs[0] as Literal;
            if (lit == null) continue;
            TypeNode templ = lit.Value as TypeNode;
            if (templ != null) {
              lit = exprs[1] as Literal;
              if (lit == null) continue;
              object[] types = lit.Value as object[];
              if (types == null) continue;
              int m = types == null ? 0 : types.Length;
              TypeNodeList templateArguments = new TypeNodeList(m);
              for (int j = 0; j < m; j++) {
                TypeNode t = types[j] as TypeNode;
                if (t == null) continue;
                templateArguments.Add(t);
              }
              this.TemplateArguments = templateArguments;
              return this.template = templ;
            }
          }
#endif
                        if (result == null)
                            template = NotSpecified;
                    }
                }
                else if (result == NotSpecified)
                {
                    return null;
                }

                return result;
            }
            set { template = value; }
        }
#if !MinimalReader
        public TypeNode TemplateExpression;
#endif
        protected TypeNodeList templateArguments;
        /// <summary>The arguments used when this (generic) type template instance was instantiated.</summary>
        public virtual TypeNodeList TemplateArguments
        {
            get
            {
                if (template == null)
                {
                    var templ = Template; //Will fill in the arguments
                    if (templ != null) templ = null;
                }

                return templateArguments;
            }
            set { templateArguments = value; }
        }
#if !MinimalReader
        public TypeNodeList TemplateArgumentExpressions;
#endif
        internal TypeNodeList consolidatedTemplateArguments;

        public virtual TypeNodeList ConsolidatedTemplateArguments
        {
            get
            {
                if (consolidatedTemplateArguments == null)
                    consolidatedTemplateArguments = GetConsolidatedTemplateArguments();
                return consolidatedTemplateArguments;
            }
            set { consolidatedTemplateArguments = value; }
        }

        private void AddTemplateParametersFromAttributeEncoding(TypeNodeList result)
        {
#if ExtendedRuntime
      if (result.Count == 0) {
        AttributeList attributes = this.Attributes;
        for (int i = 0, n = attributes == null ? 0 : attributes.Count; i < n; i++) {
          AttributeNode attr = attributes[i];
          if (attr == null) continue;
          MemberBinding mb = attr.Constructor as MemberBinding;
          if (mb == null || mb.BoundMember == null || mb.BoundMember.DeclaringType != SystemTypes.TemplateAttribute) continue;
          ExpressionList exprs = attr.Expressions;
          if (exprs == null || exprs.Count != 1) continue;
          Literal lit = exprs[0] as Literal;
          if (lit == null) continue;
          object[] types = lit.Value as object[];
          if (types == null) continue;
          for (int j = 0, m = types == null ? 0 : types.Length; j < m; j++) {
            TypeNode t = types[j] as TypeNode;
            if (t == null) continue;
            if (t.NodeType == NodeType.TypeParameter || t.NodeType == NodeType.ClassParameter)
              result.Add(t);
          }
          attributes[i] = null;
        }
      }
#endif
        }

        internal TypeNodeList templateParameters;

        /// <summary>The type parameters of this type. Null if this type is not a (generic) type template.</summary>
        public virtual TypeNodeList TemplateParameters
        {
            get
            {
                // Template parameters are populated lazily along with BaseClass and Interfaces. Trigger their population
                // via Interfaces
                var _ = Interfaces;
                var result = templateParameters;
                return result;
            }
            set
            {
                if (value == null)
                {
                    if (templateParameters == null) return;
                    if (templateParameters.Count > 0)
                        value = new TypeNodeList(0);
                }

                templateParameters = value;
            }
        }

        protected internal TypeNodeList consolidatedTemplateParameters;

        public virtual TypeNodeList ConsolidatedTemplateParameters
        {
            get
            {
                if (consolidatedTemplateParameters == null)
                    consolidatedTemplateParameters = GetConsolidatedTemplateParameters();
                return consolidatedTemplateParameters;
            }
            set { consolidatedTemplateParameters = value; }
        }

        internal ElementType typeCode = ElementType.Class;

        /// <summary>The System.TypeCode value that Convert.GetTypeCode will return pass an instance of this type as parameter.</summary>
        public virtual TypeCode TypeCode
        {
            get
            {
                switch (typeCode)
                {
                    case ElementType.Boolean: return TypeCode.Boolean;
                    case ElementType.Char: return TypeCode.Char;
                    case ElementType.Double: return TypeCode.Double;
                    case ElementType.Int16: return TypeCode.Int16;
                    case ElementType.Int32: return TypeCode.Int32;
                    case ElementType.Int64: return TypeCode.Int64;
                    case ElementType.Int8: return TypeCode.SByte;
                    case ElementType.Single: return TypeCode.Single;
                    case ElementType.UInt16: return TypeCode.UInt16;
                    case ElementType.UInt32: return TypeCode.UInt32;
                    case ElementType.UInt64: return TypeCode.UInt64;
                    case ElementType.UInt8: return TypeCode.Byte;
                    case ElementType.Void: return TypeCode.Empty;
                    default:
                        if (this == CoreSystemTypes.String) return TypeCode.String;
#if !MinimalReader
                        if (this == CoreSystemTypes.Decimal) return TypeCode.Decimal;
                        if (this == CoreSystemTypes.DateTime) return TypeCode.DateTime;
                        if (this == CoreSystemTypes.DBNull) return TypeCode.DBNull;
#endif
                        return TypeCode.Object;
                }
            }
        }

        private static readonly TypeNode NotSpecified = new Class();
#if !FxCop
        protected
#endif
            internal TrivialHashtableUsingWeakReferences structurallyEquivalentMethod;
#if !MinimalReader
        /// <summary>
        ///     Returns the methods of an abstract type that have been left unimplemented. Includes methods inherited from
        ///     base classes and interfaces, and methods from any (known) extensions.
        /// </summary>
        /// <param name="result">A method list to which the abstract methods must be appended.</param>
        public virtual void GetAbstractMethods(MethodList /*!*/ result)
        {
            if (!IsAbstract) return;
            //For each interface, get abstract methods and keep those that are not implemented by this class or a base class
            var interfaces = Interfaces;
            for (int i = 0, n = interfaces == null ? 0 : interfaces.Count; i < n; i++)
            {
                var iface = interfaces[i];
                if (iface == null) continue;
                var imembers = iface.Members;
                for (int j = 0, m = imembers == null ? 0 : imembers.Count; j < m; j++)
                {
                    var meth = imembers[j] as Method;
                    if (meth == null) continue;
                    if (ImplementsExplicitly(meth)) continue;
                    if (ImplementsMethod(meth, true)) continue;
                    result.Add(meth);
                }
            }
        }
#endif
        protected internal TrivialHashtable szArrayTypes;

        /// <summary>
        ///     Returns a type representing an array whose elements are of this type. Will always return the same instance for the
        ///     same rank.
        /// </summary>
        /// <param name="rank">The number of dimensions of the array.</param>
        public virtual ArrayType /*!*/ GetArrayType(int rank)
        {
            return GetArrayType(rank, false);
        }

        public virtual ArrayType /*!*/ GetArrayType(int rank, bool lowerBoundIsUnknown)
        {
            if (rank > 1 || lowerBoundIsUnknown) return GetArrayType(rank, 0, 0, new int[0], new int[0]);
            // assume rank == 1
            if (szArrayTypes == null) szArrayTypes = new TrivialHashtable();
            var result = (ArrayType)szArrayTypes[rank];
            if (result != null) return result;
            lock (this)
            {
                result = (ArrayType)szArrayTypes[rank];
                if (result != null) return result;
                szArrayTypes[rank] = result = new ArrayType(this, rank);
                result.Flags &= ~TypeFlags.VisibilityMask;
                result.Flags |= Flags & TypeFlags.VisibilityMask;
                return result;
            }
        }

        protected internal TrivialHashtable arrayTypes;

        /// <summary>
        ///     Returns a type representing an array whose elements are of this type. Will always return the same instance for the
        ///     same rank, sizes and bounds.
        /// </summary>
        /// <param name="rank">The number of dimensions of the array.</param>
        /// <param name="sizes">The size of each dimension.</param>
        /// <param name="loBounds">The lower bound for indices. Defaults to zero.</param>
        public virtual ArrayType /*!*/ GetArrayType(int rank, int[] sizes, int[] loBounds)
        {
            return GetArrayType(rank, sizes == null ? 0 : sizes.Length, loBounds == null ? 0 : loBounds.Length,
                sizes == null ? new int[0] : sizes, loBounds == null ? new int[0] : loBounds);
        }

        internal ArrayType /*!*/
            GetArrayType(int rank, int numSizes, int numLoBounds, int[] /*!*/ sizes, int[] /*!*/ loBounds)
        {
            if (arrayTypes == null) arrayTypes = new TrivialHashtable();
            var sb = new StringBuilder(rank * 5);
            for (var i = 0; i < rank; i++)
            {
                if (i < numLoBounds) sb.Append(loBounds[i]);
                else sb.Append('0');
                if (i < numSizes)
                {
                    sb.Append(':');
                    sb.Append(sizes[i]);
                }

                sb.Append(',');
            }

            var id = Identifier.For(sb.ToString());
            var result = (ArrayType)arrayTypes[id.UniqueIdKey];
            if (result != null) return result;
            lock (this)
            {
                result = (ArrayType)arrayTypes[id.UniqueIdKey];
                if (result != null) return result;
                if (loBounds == null) loBounds = new int[0];
                arrayTypes[id.UniqueIdKey] = result = new ArrayType(this, rank, sizes, loBounds);
                result.Flags &= ~TypeFlags.VisibilityMask;
                result.Flags |= Flags & TypeFlags.VisibilityMask;
                return result;
            }
        }

        protected internal MemberList constructors;

        public virtual MemberList GetConstructors()
        {
            if (Members.Count != memberCount) constructors = null;
            if (constructors != null) return constructors;
            lock (this)
            {
                if (constructors != null) return constructors;
                return constructors =
                    WeedOutNonSpecialMethods(GetMembersNamed(StandardIds.Ctor), MethodFlags.RTSpecialName);
            }
        }

        /// <summary>
        ///     Returns the constructor with the specified parameter types. Returns null if this type has no such constructor.
        /// </summary>
        public virtual InstanceInitializer GetConstructor(params TypeNode[] types)
        {
            return (InstanceInitializer)GetFirstMethod(GetConstructors(), types);
        }
#if !NoXml
        protected override Identifier GetDocumentationId()
        {
            if (DeclaringType == null)
                return Identifier.For("T:" + FullName);
            return Identifier.For(DeclaringType.DocumentationId + "." + Name);
        }

        internal virtual void AppendDocumentIdMangledName(StringBuilder /*!*/ sb, TypeNodeList methodTypeParameters,
            TypeNodeList typeParameters)
        {
            if (DeclaringType != null)
            {
                DeclaringType.AppendDocumentIdMangledName(sb, methodTypeParameters, typeParameters);
                sb.Append('.');
                sb.Append(GetUnmangledNameWithoutTypeParameters());
            }
            else
            {
                sb.Append(GetFullUnmangledNameWithoutTypeParameters());
            }

            var templateArguments = TemplateArguments;
            var n = templateArguments == null ? 0 : templateArguments.Count;
            if (n == 0) return;
            sb.Append('{');
            for (var i = 0; i < n; i++)
            {
                //^ assert templateArguments != null;
                var templArg = templateArguments[i];
                if (templArg == null) continue;
                templArg.AppendDocumentIdMangledName(sb, methodTypeParameters, typeParameters);
                if (i < n - 1) sb.Append(',');
            }

            sb.Append('}');
        }
#endif
        internal TrivialHashtable modifierTable;
        internal TypeNode /*!*/ GetModified(TypeNode /*!*/ modifierType, bool optionalModifier)
        {
            if (modifierTable == null) modifierTable = new TrivialHashtable();
            var result = (TypeNode)modifierTable[modifierType.UniqueKey];
            if (result != null) return result;
            result = optionalModifier
                ? new OptionalModifier(modifierType, this)
                : (TypeNode)new RequiredModifier(modifierType, this);
            modifierTable[modifierType.UniqueKey] = result;
            return result;
        }
#if CodeContracts
        [Pure]
#endif
        public static bool IsCompleteTemplate(TypeNode t)
        {
            if (t == null) return true;
            if (t.template != null) return false;
            return IsCompleteTemplate(t.DeclaringType);
        }

        public virtual TypeNode /*!*/
            GetGenericTemplateInstance(Module /*!*/ module, TypeNodeList /*!*/ consolidatedArguments)
        {
            CC.Contract.Ensures(CC.Contract.Result<TypeNode>() == null || CC.Contract.ForAll(0,
                consolidatedArguments.Count,
                i => consolidatedArguments[i] == CC.Contract.Result<TypeNode>().consolidatedTemplateArguments[i]));
            Debug.Assert(IsCompleteTemplate(this));
            if (DeclaringType == null)
                return GetTemplateInstance(module, null, null, consolidatedArguments);
            var myArgs = GetOwnTemplateArguments(consolidatedArguments);
            if (myArgs == consolidatedArguments)
                return GetTemplateInstance(module, null, DeclaringType, consolidatedArguments);
            var n = consolidatedArguments.Count;
            var m = myArgs == null ? 0 : myArgs.Count;
            var k = n - m;
            Debug.Assert(k > 0);
            var parentArgs = new TypeNodeList(k);
            for (var i = 0; i < k; i++) parentArgs.Add(consolidatedArguments[i]);
            var declaringType = DeclaringType.GetGenericTemplateInstance(module, parentArgs);
            var result = GetConsolidatedTemplateInstance(module, null, declaringType, myArgs, consolidatedArguments);
            Debug.Assert(result.DeclaringType == null || DeclaringType != null);
            Debug.Assert(result.DeclaringType != null || DeclaringType == null);

            return result;
        }
#if false
    public virtual TypeNode/*!*/ OldGetGenericTemplateInstance(Module/*!*/ module, TypeNodeList/*!*/ consolidatedArguments) {
      if (this.DeclaringType == null)
        return this.GetTemplateInstance(module, null, null, consolidatedArguments);
      TypeNodeList myArgs = this.GetOwnTemplateArguments(consolidatedArguments);
      if (myArgs == consolidatedArguments)
        return this.GetTemplateInstance(module, null, this.DeclaringType, consolidatedArguments);
      int n = consolidatedArguments.Count;
      int m = myArgs == null ? 0 : myArgs.Count;
      int k = n - m;
      Debug.Assert(k > 0);
      TypeNodeList parentArgs = new TypeNodeList(k);
      for (int i = 0; i < k; i++) parentArgs.Add(consolidatedArguments[i]);
      TypeNode declaringType = this.DeclaringType.GetGenericTemplateInstance(module, parentArgs);
      TypeNode nestedType = declaringType.GetNestedType(this.Name);
      if (nestedType == null) {
        // can happen if a new nested type is added to a template
        // see if the type appears in the original parent template
        TypeNode declaringTypeTemplate = declaringType.Template;
        TypeNode nestedTemplate = declaringTypeTemplate.GetNestedType(this.Name);
        
        Duplicator duplicator = new Duplicator(module, null);
        duplicator.RecordOriginalAsTemplate = true;
        duplicator.SkipBodies = true;
        duplicator.TypesToBeDuplicated[this.UniqueKey] = this;
        nestedType = duplicator.VisitTypeNode(this, null, null, null, true);
        Specializer specializer =
 new Specializer(module, declaringTypeTemplate.ConsolidatedTemplateParameters, declaringType.ConsolidatedTemplateArguments);
        specializer.VisitTypeNode(nestedType);

        nestedType.DeclaringType = declaringType;
        declaringType.NestedTypes.Add(nestedType);

        // Debug.Fail("template declaring type dummy instance does not have a nested type corresponding to template"); nestedType = this;
      }
      if (m == 0){Debug.Assert(nestedType.template != null); return nestedType;}
      return nestedType.GetTemplateInstance(module, null, declaringType, myArgs);
    }
#endif
        public virtual TypeNode /*!*/ GetTemplateInstance(Module module, params TypeNode[] typeArguments)
        {
            return GetTemplateInstance(module, null, null, new TypeNodeList(typeArguments));
        }

        protected virtual TypeNode TryToFindExistingInstance(Module /*!*/ module, Identifier /*!*/ uniqueMangledName)
        {
            return module.TryGetTemplateInstance(uniqueMangledName);
        }

        private Identifier /*!*/ GetUniqueMangledTemplateInstanceName(TypeNodeList /*!*/ templateArguments)
        {
            return GetUniqueMangledTemplateInstanceName(UniqueKey, templateArguments);
        }

        internal static Identifier /*!*/
            GetUniqueMangledTemplateInstanceName(int templateId, TypeNodeList /*!*/ templateArguments)
        {
            var strings = new string[1 + templateArguments.Count];
            strings[0] = templateId.ToString();
            for (int i = 0, n = templateArguments.Count; i < n; i++)
            {
                var t = templateArguments[i];
                if (t == null || t.Name == null) continue;
                strings[i + 1] = t.UniqueKey.ToString();
            }

            return Identifier.For(string.Join(":", strings));
        }

        public virtual Identifier /*!*/
            GetMangledTemplateInstanceName(TypeNodeList /*!*/ templateArguments, out bool notFullySpecialized)
        {
            var mangledNameBuilder = new StringBuilder(Name.ToString());
            notFullySpecialized = false;
            for (int i = 0, n = templateArguments.Count; i < n; i++)
            {
                if (i == 0) mangledNameBuilder.Append('<');
                var t = templateArguments[i];
                if (t == null || t.Name == null) continue;
                //if (TypeIsNotFullySpecialized(t)) notFullySpecialized = true;
                mangledNameBuilder.Append(t.FullName);
                if (i < n - 1)
                    mangledNameBuilder.Append(',');
                else
                    mangledNameBuilder.Append('>');
            }

            return Identifier.For(mangledNameBuilder.ToString());
        }

        private static bool TypeIsNotFullySpecialized(TypeNode t)
        {
            var tt = StripModifiers(t);
            //^ assert tt != null;        
            if (tt is TypeParameter || tt is ClassParameter || tt.IsNotFullySpecialized)
                return true;
            for (int j = 0, m = tt.StructuralElementTypes == null ? 0 : tt.StructuralElementTypes.Count; j < m; j++)
            {
                var et = tt.StructuralElementTypes[j];
                if (et == null) continue;
                if (TypeIsNotFullySpecialized(et)) return true;
            }

            return false;
        }

        /// <summary>
        ///     Gets an instance for the given template arguments of this (generic) template type.
        /// </summary>
        /// <param name="referringType">
        ///     The type in which the reference to the template instance occurs. If the template is not
        ///     generic, the instance becomes a nested type of the referring type so that it has the same access privileges as the
        ///     code referrring to the instance.
        /// </param>
        /// <param name="templateArguments">The template arguments.</param>
        /// <returns>An instance of the template. Always the same instance for the same arguments.</returns>
        public virtual TypeNode /*!*/ GetTemplateInstance(TypeNode referringType, params TypeNode[] templateArguments)
        {
            return GetTemplateInstance(referringType, new TypeNodeList(templateArguments));
        }

        /// <summary>
        ///     Gets an instance for the given template arguments of this (generic) template type.
        /// </summary>
        /// <param name="referringType">
        ///     The type in which the reference to the template instance occurs. If the template is not
        ///     generic, the instance becomes a nested type of the referring type so that it has the same access privileges as the
        ///     code referrring to the instance.
        /// </param>
        /// <param name="templateArguments">The template arguments.</param>
        /// <returns>An instance of the template. Always the same instance for the same arguments.</returns>
        public virtual TypeNode /*!*/ GetTemplateInstance(TypeNode referringType, TypeNodeList templateArguments)
        {
            if (referringType == null) return this;
            var module = referringType.DeclaringModule;
            return GetTemplateInstance(module, referringType, DeclaringType, templateArguments);
        }

        private class CachingModuleForGenericsInstances : Module
        {
            public override TypeNode GetStructurallyEquivalentType(Identifier ns, Identifier /*!*/ id,
                Identifier uniqueMangledName, bool lookInReferencedAssemblies)
            {
                if (uniqueMangledName == null) return null;
                return (TypeNode)StructurallyEquivalentType[uniqueMangledName.UniqueIdKey];
            }
        }

        private static int CompareArgs(TypeNodeList templateArguments, TypeNodeList typeNodeList)
        {
            for (var i = 0; i < templateArguments.Count; i++)
            {
                if (templateArguments[i].UniqueKey < typeNodeList[i].UniqueKey) return -1;
                if (templateArguments[i].UniqueKey > typeNodeList[i].UniqueKey) return 1;
            }

            return 0;
        }

        private struct TemplateInstanceCache
        {
            private struct TemplateInstanceEntry
            {
                public TypeNodeList TemplateArguments;
                public TypeNode Instance;
            }

            public int count;

            /// <summary>
            ///     Sorted lexicographically by templateargument uniqueId
            /// </summary>
            private TemplateInstanceEntry[] cache;

            public int Count => count;

            public TypeNode Find(TypeNodeList args)
            {
                if (cache == null) return null;
                return BinarySearch(args);
            }

            private TypeNode BinarySearch(TypeNodeList templateArguments)
            {
                CC.Contract.Requires(cache != null);
                CC.Contract.Ensures(CC.Contract.Result<TypeNode>() == null || CC.Contract.ForAll(0,
                    templateArguments.Count,
                    i => templateArguments[i] == CC.Contract.Result<TypeNode>().consolidatedTemplateArguments[i]));

                var lo = 0;
                var hi = count - 1;
                while (lo <= hi)
                {
                    var mid = (lo + hi) / 2;
                    var comp = CompareArgs(templateArguments, cache[mid].TemplateArguments);
                    if (comp == 0) return cache[mid].Instance; // found
                    if (comp < 0)
                    {
                        hi = mid - 1;
                        continue;
                    }

                    lo = mid + 1;
                }

                return null;
            }

            internal void Insert(TypeNodeList templateArguments, TypeNode instance)
            {
                if (cache == null)
                {
                    cache = new TemplateInstanceEntry[4];
                    cache[0].TemplateArguments = templateArguments;
                    cache[0].Instance = instance;
                    count++;
                    return;
                }

                if (count == cache.Length)
                {
                    // increase
                    var old = cache;
                    cache = new TemplateInstanceEntry[count + 8];
                    Array.Copy(old, cache, count);
                }

                // insert sorted
                var lo = 0;
                var hi = count - 1;
                var insertPos = -1;
                while (lo <= hi)
                {
                    var mid = (lo + hi) / 2;
                    var comp = CompareArgs(templateArguments, cache[mid].TemplateArguments);
                    if (comp == 0) throw new InvalidOperationException("inserting template that is present");
                    if (comp < 0)
                    {
                        hi = mid - 1;
                        if (hi < lo)
                        {
                            insertPos = mid;
                            break;
                        }

                        continue;
                    }

                    lo = mid + 1;
                    if (hi < lo) insertPos = mid + 1;
                }

                if (insertPos < 0) throw new InvalidOperationException("internal error in template cache");
                // move remaining elements
                for (var i = count - 1; i >= insertPos; i--) cache[i + 1] = cache[i];
                cache[insertPos].TemplateArguments = templateArguments;
                cache[insertPos].Instance = instance;
                count++;
            }
        }

        private TemplateInstanceCache templateInstanceCache;


        private TypeNode TryToFindExistingInstance(TypeNodeList consolidatedTemplateArguments)
        {
            CC.Contract.Requires(consolidatedTemplateArguments.Count == ConsolidatedTemplateParameters.Count);

            return templateInstanceCache.Find(consolidatedTemplateArguments);
        }

        private void AddInstance(TypeNodeList consolidatedTemplateArguments, TypeNode instance)
        {
            CC.Contract.Requires(consolidatedTemplateArguments.Count == instance.ConsolidatedTemplateArguments.Count);
            CC.Contract.Requires(consolidatedTemplateArguments.Count == ConsolidatedTemplateParameters.Count);

            templateInstanceCache.Insert(consolidatedTemplateArguments, instance);
        }

        internal void ClearTemplateInstanceCache()
        {
            templateInstanceCache = new TemplateInstanceCache();
        }

        private TypeNodeList currentlyInstantiating;

        //protected static Module/*!*/ cachingModuleForGenericInstances = new CachingModuleForGenericsInstances();
        public virtual TypeNode /*!*/ GetConsolidatedTemplateInstance(Module module, TypeNode referringType,
            TypeNode declaringType, TypeNodeList templateArguments, TypeNodeList consolidatedTemplateArguments)
        {
            Debug.Assert(IsCompleteTemplate(this));
            Debug.Assert(ConsolidatedTemplateParameters.Count == consolidatedTemplateArguments.Count);
            Debug.Assert(declaringType == null || DeclaringType != null);
            Debug.Assert(declaringType != null || DeclaringType == null);
            Debug.Assert(declaringType == null || DeclaringType == declaringType ||
                         DeclaringType == declaringType.Template);

            var templateParameters = TemplateParameters;
            if (module == null ||
                (declaringType == null && (templateParameters == null || templateParameters.Count == 0)))
            {
                Debug.Assert(false);
                return this;
            }

            if (IsGeneric)
                referringType = null;
            //module = TypeNode.cachingModuleForGenericInstances;
            //Identifier/*!*/ uniqueMangledName = this.GetUniqueMangledTemplateInstanceName(consolidatedTemplateArguments);
            //TypeNode result = this.TryToFindExistingInstance(module, uniqueMangledName);
            //if (result != null) return result;
            if (NewTemplateInstanceIsRecursive)
                return this; //An instance of this template is trying to instantiate the template again
            lock (this)
            {
                var result = TryToFindExistingInstance(consolidatedTemplateArguments);
                if (result != null) return result;

                var savedOldInstantiating = currentlyInstantiating;
                if (savedOldInstantiating != null &&
                    CompareArgs(savedOldInstantiating, consolidatedTemplateArguments) == 0) throw new Exception();
                currentlyInstantiating = consolidatedTemplateArguments;

                result = BuildConsolidatedTemplateInstance(module, referringType, declaringType, templateArguments,
                    consolidatedTemplateArguments);

                currentlyInstantiating = savedOldInstantiating;

                return result;
            }
        }

        public virtual TypeNode /*!*/ GetTemplateInstance(Module module, TypeNode referringType, TypeNode declaringType,
            TypeNodeList templateArguments)
        {
            return GetConsolidatedTemplateInstance(module, referringType, declaringType, templateArguments,
                templateArguments);
        }

        private TypeNode BuildConsolidatedTemplateInstance(Module module, TypeNode referringType,
            TypeNode declaringType, TypeNodeList templateArguments, TypeNodeList consolidatedTemplateArguments)
        {
            CC.Contract.Ensures(CC.Contract.Result<TypeNode>() == null || CC.Contract.ForAll(0, templateArguments.Count,
                i => templateArguments[i] == CC.Contract.Result<TypeNode>().templateArguments[i]));
            CC.Contract.Ensures(CC.Contract.Result<TypeNode>() == null || CC.Contract.ForAll(0,
                consolidatedTemplateArguments.Count,
                i => consolidatedTemplateArguments[i] ==
                     CC.Contract.Result<TypeNode>().consolidatedTemplateArguments[i]));

            CC.Contract.Assume(IsCompleteTemplate(this));
            CC.Contract.Assume(ConsolidatedTemplateParameters.Count == consolidatedTemplateArguments.Count);
            CC.Contract.Assume(declaringType == null || DeclaringType != null);
            CC.Contract.Assume(declaringType != null || DeclaringType == null);
            CC.Contract.Assume(declaringType == null || DeclaringType == declaringType ||
                               DeclaringType == declaringType.Template);

            var duplicator = new Duplicator(module, declaringType);
            duplicator.RecordOriginalAsTemplate = true;
            duplicator.SkipBodies = true;
            duplicator.TypesToBeDuplicated[UniqueKey] = this;
            var result = duplicator.VisitTypeNode(this, null, templateArguments, this, true);
            //^ assume result != null;

            result.Name.SourceContext = Name.SourceContext;
            result.fullName = null;
            if (IsGeneric) result.DeclaringModule = DeclaringModule;
            result.DeclaringType = IsGeneric || referringType == null ? declaringType : referringType;
            result.Template = this;
            result.templateParameters = null; // new TypeNodeList(0);
            result.consolidatedTemplateParameters = null; // new TypeNodeList(0);
            result.templateArguments = templateArguments;
            result.consolidatedTemplateArguments = consolidatedTemplateArguments;
            //module.StructurallyEquivalentType[unusedMangledName.UniqueIdKey] = result;
            //module.StructurallyEquivalentType[uniqueMangledName.UniqueIdKey] = result;
            AddInstance(consolidatedTemplateArguments, result);

            bool notFullySpecialized;
            result.Name = GetMangledTemplateInstanceName(templateArguments, out notFullySpecialized);
            result.IsNotFullySpecialized = notFullySpecialized ||
                                           (declaringType != null && TypeIsNotFullySpecialized(declaringType));
            //^ assume unusedMangledName != null;

            var specializer = new Specializer(module, ConsolidatedTemplateParameters, consolidatedTemplateArguments);
            specializer.VisitTypeNode(result);
            var visibility = Flags & TypeFlags.VisibilityMask;
            for (int i = 0, m = templateArguments.Count; i < m; i++)
            {
                var t = templateArguments[i];
                if (t == null) continue;
                //if (TypeIsNotFullySpecialized(t)) continue;
                visibility = GetVisibilityIntersection(visibility, t.Flags & TypeFlags.VisibilityMask);
            }

            result.Flags &= ~TypeFlags.VisibilityMask;
            result.Flags |= visibility;
            // Can't touch DeclaringType here as this will trigger specializer/dup providers that we don't yet want to trigger.
            //Debug.Assert(result.DeclaringType == null || this.DeclaringType != null);
            //Debug.Assert(result.DeclaringType != null || this.DeclaringType == null);
            return result;
        }

        protected virtual TypeNodeList GetConsolidatedTemplateArguments()
        {
            var typeArgs = TemplateArguments;
            if (DeclaringType == null) return typeArgs;
            var result = DeclaringType.ConsolidatedTemplateArguments;
            if (result == null)
            {
                if (DeclaringType.IsGeneric && DeclaringType.Template == null)
                    result = DeclaringType.ConsolidatedTemplateParameters;
                if (result == null)
                    return typeArgs;
            }

            var n = typeArgs == null ? 0 : typeArgs.Count;
            if (n == 0) return result;
            result = result.Clone();
            for (var i = 0; i < n; i++) result.Add(typeArgs[i]);
            return result;
        }

        protected virtual TypeNodeList GetConsolidatedTemplateArguments(TypeNodeList typeArgs)
        {
            var result = ConsolidatedTemplateArguments;
            if (result == null || result.Count == 0)
            {
                if (IsGeneric && Template == null)
                    result = ConsolidatedTemplateParameters;
                else
                    return typeArgs;
            }

            var n = typeArgs == null ? 0 : typeArgs.Count;
            if (n == 0) return result;
            //^ assert typeArgs != null;
            result = result.Clone();
            for (var i = 0; i < n; i++) result.Add(typeArgs[i]);
            return result;
        }

        protected virtual TypeNodeList GetConsolidatedTemplateParameters()
        {
            var typeParams = TemplateParameters;
            var declaringType = DeclaringType;
            if (declaringType == null) return typeParams;
            while (declaringType.Template != null) declaringType = declaringType.Template;
            var result = declaringType.ConsolidatedTemplateParameters;
            if (result == null) return typeParams;
            var n = typeParams == null ? 0 : typeParams.Count;
            if (n == 0) return result;
            result = result.Clone();
            for (var i = 0; i < n; i++) result.Add(typeParams[i]);
            return result;
        }

        protected virtual TypeNodeList GetOwnTemplateArguments(TypeNodeList consolidatedTemplateArguments)
        {
            var n = TemplateParameters == null ? 0 : TemplateParameters.Count;
            var m = consolidatedTemplateArguments == null ? 0 : consolidatedTemplateArguments.Count;
            var k = m - n;
            if (k <= 0) return consolidatedTemplateArguments;
            var result = new TypeNodeList(n);
            if (consolidatedTemplateArguments != null)
                for (var i = 0; i < n; i++)
                    result.Add(consolidatedTemplateArguments[i + k]);
            return result;
        }

        public TypeNode SelfInstantiation()
        {
            if (Template != null) return this;
            var ownArgs = ConsolidatedTemplateParameters;
            if (ownArgs == null || ownArgs.Count == 0) return this;
            return GetGenericTemplateInstance(DeclaringModule, ownArgs);
        }
#if ExtendedRuntime
    private static MemberBinding templateInstanceAttribute = null;
#endif
        protected internal Pointer pointerType;

        public virtual Pointer /*!*/ GetPointerType()
        {
            var result = pointerType;
            if (result == null)
                lock (this)
                {
                    if (pointerType != null) return pointerType;
                    result = pointerType = new Pointer(this);
                    result.Flags &= ~TypeFlags.VisibilityMask;
                    result.Flags |= Flags & TypeFlags.VisibilityMask;
                    result.DeclaringModule = DeclaringModule;
                }

            return result;
        }

        protected internal Reference referenceType;

        public virtual Reference /*!*/ GetReferenceType()
        {
            var result = referenceType;
            if (result == null)
                lock (this)
                {
                    if (referenceType != null) return referenceType;
                    result = referenceType = new Reference(this);
                    result.Flags &= ~TypeFlags.VisibilityMask;
                    result.Flags |= Flags & TypeFlags.VisibilityMask;
                    result.DeclaringModule = DeclaringModule;
                }

            return result;
        }

        //^ [Microsoft.Contracts.SpecPublic]
        protected internal TrivialHashtable memberTable;
        protected internal int memberCount;

        /// <summary>
        ///     Returns a list of all the members declared directly by this type with the specified name.
        ///     Returns an empty list if this type has no such members.
        /// </summary>
        public virtual MemberList /*!*/ GetMembersNamed(Identifier name)
        {
#if CodeContracts
            CC.Contract.Ensures(CC.Contract.Result<MemberList>() != null);
#endif
            if (name == null) return new MemberList(0);
            var members = Members;
            var n = members == null ? 0 : members.Count;
            if (n != memberCount || memberTable == null) UpdateMemberTable(n);
            //^ assert this.memberTable != null;
            var result = (MemberList)memberTable[name.UniqueIdKey];
            if (result == null)
                lock (this)
                {
                    result = (MemberList)memberTable[name.UniqueIdKey];
                    if (result != null) return result;
                    memberTable[name.UniqueIdKey] = result = new MemberList();
                }

            return result;
        }

        /// <summary>
        ///     Returns the first event declared by this type with the specified name.
        ///     Returns null if this type has no such event.
        /// </summary>
        public virtual Event GetEvent(Identifier name)
        {
            var members = GetMembersNamed(name);
            for (int i = 0, n = members.Count; i < n; i++)
            {
                var ev = members[i] as Event;
                if (ev != null) return ev;
            }

            return null;
        }

        /// <summary>
        ///     Returns the first field declared by this type with the specified name. Returns null if this type has no such field.
        /// </summary>
        public virtual Field GetField(Identifier name)
        {
            var members = GetMembersNamed(name);
            for (int i = 0, n = members.Count; i < n; i++)
            {
                var field = members[i] as Field;
                if (field != null) return field;
            }

            return null;
        }

        /// <summary>
        ///     Returns the first method declared by this type with the specified name and parameter types. Returns null if this
        ///     type has no such method.
        /// </summary>
        /// <returns></returns>
        public virtual Method GetMethod(Identifier name, params TypeNode[] types)
        {
            return GetFirstMethod(GetMembersNamed(name), types);
        }

        private static Method GetFirstMethod(MemberList members, params TypeNode[] types)
        {
            if (members == null) return null;
            var m = types == null ? 0 : types.Length;
            var typeNodes = m == 0 ? null : new TypeNodeList(types);
            for (int i = 0, n = members.Count; i < n; i++)
            {
                var meth = members[i] as Method;
                if (meth == null) continue;
                if (meth.ParameterTypesMatchStructurally(typeNodes)) return meth;
            }

            return null;
        }

        public virtual MethodList GetMethods(Identifier name, params TypeNode[] types)
        {
            return GetMethods(GetMembersNamed(name), types);
        }

        private static MethodList GetMethods(MemberList members, params TypeNode[] types)
        {
            if (members == null) return null;
            var m = types == null ? 0 : types.Length;
            var result = new MethodList();
            var typeNodes = m == 0 ? null : new TypeNodeList(types);
            for (int i = 0, n = members.Count; i < n; i++)
            {
                var meth = members[i] as Method;
                if (meth == null) continue;
                if (meth.ParameterTypesMatchStructurally(typeNodes)) result.Add(meth);
            }

            return result;
        }

        public Method GetMatchingMethod(Method method)
        {
            if (method == null || method.Name == null) return null;
            var members = GetMembersNamed(method.Name);
            for (int i = 0, n = members == null ? 0 : members.Count; i < n; i++)
            {
                var m = members[i] as Method;
                if (m == null) continue;
                if (m.ParametersMatchStructurally(method.Parameters)) return m;
            }

            return null;
        }

        public Method GetExactMatchingMethod(Method method)
        {
            if (method == null || method.Name == null) return null;
            var methodTPcount = method.TemplateParameters == null ? 0 : method.TemplateParameters.Count;
            var members = GetMembersNamed(method.Name);
            for (int i = 0, n = members == null ? 0 : members.Count; i < n; i++)
            {
                var m = members[i] as Method;
                if (m == null) continue;
                var mTPcount = m.TemplateParameters == null ? 0 : m.TemplateParameters.Count;
                if (mTPcount != methodTPcount) continue;
                if (m.ReturnType.IsStructurallyEquivalentTo(method.ReturnType) &&
                    m.ParametersMatchStructurally(method.Parameters)) return m;
            }

            return null;
        }

        /// <summary>
        ///     Returns the first nested type declared by this type with the specified name. Returns null if this type has no such
        ///     nested type.
        /// </summary>
        public virtual TypeNode GetNestedType(Identifier name)
        {
            if (name == null) return null;
            if (template != null)
            {
                Debug.Assert(false);
                throw new InvalidOperationException();
            }

            if (this.members != null)
            {
                var members = GetMembersNamed(name);
                for (int i = 0, n = members.Count; i < n; i++)
                {
                    var type = members[i] as TypeNode;
                    if (type != null) return type;
                }

                return null;
            }

            var nestedTypes = NestedTypes;
            for (int i = 0, n = nestedTypes == null ? 0 : nestedTypes.Count; i < n; i++)
            {
                var type = nestedTypes[i];
                if (type != null && type.Name.UniqueIdKey == name.UniqueIdKey) return type;
            }

            return null;
        }

        protected internal TypeNodeList nestedTypes;

        public virtual TypeNodeList NestedTypes
        {
            get
            {
                if (this.nestedTypes != null && (this.members == null || this.members.Count == memberCount))
                    return this.nestedTypes;
                if (ProvideNestedTypes != null && ProviderHandle != null)
                {
                    lock (Module.GlobalLock)
                    {
                        ProvideNestedTypes(this, ProviderHandle);
                    }
                }
                else
                {
                    var members = Members;
                    var nestedTypes = new TypeNodeList();
                    for (int i = 0, n = members == null ? 0 : members.Count; i < n; i++)
                    {
                        var nt = members[i] as TypeNode;
                        if (nt == null) continue;
                        nestedTypes.Add(nt);
                    }

                    this.nestedTypes = nestedTypes;
                }

                return this.nestedTypes;
            }
            set { nestedTypes = value; }
        }

        /// <summary>
        ///     Returns the first property declared by this type with the specified name and parameter types. Returns null if this
        ///     type has no such property.
        /// </summary>
        public virtual Property GetProperty(Identifier name, params TypeNode[] types)
        {
            return GetProperty(GetMembersNamed(name), types);
        }

        private static Property GetProperty(MemberList members, params TypeNode[] types)
        {
            if (members == null) return null;
            var m = types == null ? 0 : types.Length;
            var typeNodes = m == 0 ? null : new TypeNodeList(types);
            for (int i = 0, n = members.Count; i < n; i++)
            {
                var prop = members[i] as Property;
                if (prop == null) continue;
                if (prop.ParameterTypesMatch(typeNodes)) return prop;
            }

            return null;
        }
#if !MinimalReader
        protected internal MemberList explicitCoercionMethods;

        public virtual MemberList ExplicitCoercionMethods
        {
            get
            {
                if (Members.Count != memberCount) explicitCoercionMethods = null;
                if (explicitCoercionMethods != null) return explicitCoercionMethods;
                lock (this)
                {
                    if (explicitCoercionMethods != null) return explicitCoercionMethods;
                    return explicitCoercionMethods = WeedOutNonSpecialMethods(GetMembersNamed(StandardIds.opExplicit),
                        MethodFlags.SpecialName);
                }
            }
        }

        protected internal MemberList implicitCoercionMethods;

        public virtual MemberList ImplicitCoercionMethods
        {
            get
            {
                if (Members.Count != memberCount) implicitCoercionMethods = null;
                if (implicitCoercionMethods != null) return implicitCoercionMethods;
                lock (this)
                {
                    if (implicitCoercionMethods != null) return implicitCoercionMethods;
                    return implicitCoercionMethods = WeedOutNonSpecialMethods(GetMembersNamed(StandardIds.opImplicit),
                        MethodFlags.SpecialName);
                }
            }
        }

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        protected static readonly Method MethodDoesNotExist = new Method();

        protected internal TrivialHashtable explicitCoercionFromTable;

        public virtual Method GetExplicitCoercionFromMethod(TypeNode sourceType)
        {
            if (sourceType == null) return null;
            Method result = null;
            if (explicitCoercionFromTable != null)
                result = (Method)explicitCoercionFromTable[sourceType.UniqueKey];
            if (result == MethodDoesNotExist) return null;
            if (result != null) return result;
            lock (this)
            {
                if (explicitCoercionFromTable != null)
                    result = (Method)explicitCoercionFromTable[sourceType.UniqueKey];
                if (result == MethodDoesNotExist) return null;
                if (result != null) return result;
                var coercions = ExplicitCoercionMethods;
                for (int i = 0, n = coercions.Count; i < n; i++)
                {
                    var m = (Method)coercions[i];
                    if (sourceType == m.Parameters[0].Type)
                    {
                        result = m;
                        break;
                    }
                }

                if (explicitCoercionFromTable == null)
                    explicitCoercionFromTable = new TrivialHashtable();
                if (result == null)
                    explicitCoercionFromTable[sourceType.UniqueKey] = MethodDoesNotExist;
                else
                    explicitCoercionFromTable[sourceType.UniqueKey] = result;
                return result;
            }
        }

        protected internal TrivialHashtable explicitCoercionToTable;

        public virtual Method GetExplicitCoercionToMethod(TypeNode targetType)
        {
            if (targetType == null) return null;
            Method result = null;
            if (explicitCoercionToTable != null)
                result = (Method)explicitCoercionToTable[targetType.UniqueKey];
            if (result == MethodDoesNotExist) return null;
            if (result != null) return result;
            lock (this)
            {
                if (explicitCoercionToTable != null)
                    result = (Method)explicitCoercionToTable[targetType.UniqueKey];
                if (result == MethodDoesNotExist) return null;
                if (result != null) return result;
                var coercions = ExplicitCoercionMethods;
                for (int i = 0, n = coercions.Count; i < n; i++)
                {
                    var m = (Method)coercions[i];
                    if (m.ReturnType == targetType)
                    {
                        result = m;
                        break;
                    }
                }

                if (explicitCoercionToTable == null)
                    explicitCoercionToTable = new TrivialHashtable();
                if (result == null)
                    explicitCoercionToTable[targetType.UniqueKey] = MethodDoesNotExist;
                else
                    explicitCoercionToTable[targetType.UniqueKey] = result;
            }

            return result;
        }

        protected internal TrivialHashtable implicitCoercionFromTable;

        public virtual Method GetImplicitCoercionFromMethod(TypeNode sourceType)
        {
            if (sourceType == null) return null;
            Method result = null;
            if (implicitCoercionFromTable != null)
                result = (Method)implicitCoercionFromTable[sourceType.UniqueKey];
            if (result == MethodDoesNotExist) return null;
            if (result != null) return result;
            lock (this)
            {
                if (implicitCoercionFromTable != null)
                    result = (Method)implicitCoercionFromTable[sourceType.UniqueKey];
                if (result == MethodDoesNotExist) return null;
                if (result != null) return result;
                var coercions = ImplicitCoercionMethods;
                for (int i = 0, n = coercions.Count; i < n; i++)
                {
                    var m = (Method)coercions[i];
                    if (sourceType.IsStructurallyEquivalentTo(StripModifiers(m.Parameters[0].Type)))
                    {
                        result = m;
                        break;
                    }
                }

                if (implicitCoercionFromTable == null)
                    implicitCoercionFromTable = new TrivialHashtable();
                if (result == null)
                    implicitCoercionFromTable[sourceType.UniqueKey] = MethodDoesNotExist;
                else
                    implicitCoercionFromTable[sourceType.UniqueKey] = result;
                return result;
            }
        }

        protected internal TrivialHashtable implicitCoercionToTable;

        public virtual Method GetImplicitCoercionToMethod(TypeNode targetType)
        {
            if (targetType == null) return null;
            Method result = null;
            if (implicitCoercionToTable != null)
                result = (Method)implicitCoercionToTable[targetType.UniqueKey];
            if (result == MethodDoesNotExist) return null;
            if (result != null) return result;
            lock (this)
            {
                if (implicitCoercionToTable != null)
                    result = (Method)implicitCoercionToTable[targetType.UniqueKey];
                if (result == MethodDoesNotExist) return null;
                if (result != null) return result;
                var coercions = ImplicitCoercionMethods;
                for (int i = 0, n = coercions.Count; i < n; i++)
                {
                    var m = (Method)coercions[i];
                    if (m.ReturnType == targetType)
                    {
                        result = m;
                        break;
                    }
                }

                if (implicitCoercionToTable == null)
                    implicitCoercionToTable = new TrivialHashtable();
                if (result == null)
                    implicitCoercionToTable[targetType.UniqueKey] = MethodDoesNotExist;
                else
                    implicitCoercionToTable[targetType.UniqueKey] = result;
                return result;
            }
        }

        protected Method opFalse;

        public virtual Method GetOpFalse()
        {
            var result = this.opFalse;
            if (result == MethodDoesNotExist) return null;
            if (result != null) return result;
            var members = Members; //evaluate for side effect
            if (members != null) members = null;
            lock (this)
            {
                result = this.opFalse;
                if (result == MethodDoesNotExist) return null;
                if (result != null) return result;
                var t = this;
                while (t != null)
                {
                    var opFalses = t.GetMembersNamed(StandardIds.opFalse);
                    if (opFalses != null)
                        for (int i = 0, n = opFalses.Count; i < n; i++)
                        {
                            var opFalse = opFalses[i] as Method;
                            if (opFalse == null) continue;
                            if (!opFalse.IsSpecialName || !opFalse.IsStatic || !opFalse.IsPublic ||
                                opFalse.ReturnType != CoreSystemTypes.Boolean ||
                                opFalse.Parameters == null || opFalse.Parameters.Count != 1) continue;
                            return this.opFalse = opFalse;
                        }

                    t = t.BaseType;
                }

                this.opFalse = MethodDoesNotExist;
                return null;
            }
        }

        protected Method opTrue;
        public virtual Method GetOpTrue()
        {
            var result = this.opTrue;
            if (result == MethodDoesNotExist) return null;
            if (result != null) return result;
            var members = Members; //evaluate for side effect
            if (members != null) members = null;
            lock (this)
            {
                result = this.opTrue;
                if (result == MethodDoesNotExist) return null;
                if (result != null) return result;
                var t = this;
                while (t != null)
                {
                    var opTrues = t.GetMembersNamed(StandardIds.opTrue);
                    if (opTrues != null)
                        for (int i = 0, n = opTrues.Count; i < n; i++)
                        {
                            var opTrue = opTrues[i] as Method;
                            if (opTrue == null) continue;
                            if (!opTrue.IsSpecialName || !opTrue.IsStatic || !opTrue.IsPublic ||
                                opTrue.ReturnType != CoreSystemTypes.Boolean ||
                                opTrue.Parameters == null || opTrue.Parameters.Count != 1) continue;
                            return this.opTrue = opTrue;
                        }

                    t = t.BaseType;
                }

                this.opTrue = MethodDoesNotExist;
                return null;
            }
        }
#endif
#if !NoReflection
        private static Hashtable typeMap; //contains weak references

        /// <summary>
        ///     Gets a TypeNode instance corresponding to the given System.Type instance.
        /// </summary>
        /// <param name="type">A runtime type.</param>
        /// <returns>A TypeNode instance.</returns>
        public static TypeNode GetTypeNode(Type type)
        {
            if (type == null) return null;
            var typeMap = TypeNode.typeMap;
            if (typeMap == null) TypeNode.typeMap = typeMap = Hashtable.Synchronized(new Hashtable());
            TypeNode result = null;
            var wr = (WeakReference)typeMap[type];
            if (wr != null)
            {
                result = wr.Target as TypeNode;
                if (result == Class.DoesNotExist) return null;
                if (result != null) return result;
            }
#if WHIDBEY
            if (type.IsGenericType && !type.IsGenericTypeDefinition)
                try
                {
                    var template = GetTypeNode(type.GetGenericTypeDefinition());
                    if (template == null) return null;
                    var templateArguments = new TypeNodeList();
                    foreach (var arg in type.GetGenericArguments())
                        templateArguments.Add(GetTypeNode(arg));
                    return template.GetGenericTemplateInstance(template.DeclaringModule, templateArguments);
                }
                catch
                {
                    //TODO: log error
                    return null;
                }

            if (type.IsGenericParameter)
                try
                {
                    var parIndx = type.GenericParameterPosition;
                    var mi = type.DeclaringMethod as Reflection.MethodInfo;
                    if (mi != null)
                    {
                        var m = Method.GetMethod(mi);
                        if (m == null) return null;
                        if (m.TemplateParameters != null && m.TemplateParameters.Count > parIndx)
                            return m.TemplateParameters[parIndx];
                    }
                    else
                    {
                        var ti = type.DeclaringType;
                        var t = GetTypeNode(ti);
                        if (t == null) return null;
                        if (t.TemplateParameters != null && t.TemplateParameters.Count > parIndx)
                            return t.TemplateParameters[parIndx];
                    }

                    return null;
                }
                catch
                {
                    //TODO: log error
                    return null;
                }
#endif
            if (type.HasElementType)
            {
                var elemType = GetTypeNode(type.GetElementType());
                if (elemType == null) return null;
                if (type.IsArray)
                {
                    result = elemType.GetArrayType(type.GetArrayRank());
                }
                else if (type.IsByRef)
                {
                    result = elemType.GetReferenceType();
                }
                else if (type.IsPointer)
                {
                    result = elemType.GetPointerType();
                }
                else
                {
                    Debug.Assert(false);
                    result = null;
                }
            }
            else if (type.DeclaringType != null)
            {
                var dType = GetTypeNode(type.DeclaringType);
                if (dType == null) return null;
                result = dType.GetNestedType(Identifier.For(type.Name));
            }
            else
            {
                var assem = AssemblyNode.GetAssembly(type.Assembly);
                if (assem != null) result = assem.GetType(Identifier.For(type.Namespace), Identifier.For(type.Name));
            }

            if (result == null)
                typeMap[type] = new WeakReference(Class.DoesNotExist);
            else
                typeMap[type] = new WeakReference(result);
            return result;
        }

        protected internal Type runtimeType;
        /// <summary>
        ///     Gets a System.Type instance corresponding to this type. The assembly containin this type must be normalized
        ///     and must have a location on disk or must have been loaded via AssemblyNode.GetRuntimeAssembly.
        /// </summary>
        /// <returns>A System.Type instance. (A runtime type.)</returns>
        public virtual Type GetRuntimeType()
        {
            if (runtimeType == null)
                lock (this)
                {
                    if (runtimeType != null) return runtimeType;
#if WHIDBEY
                    if (IsGeneric && Template != null)
                        try
                        {
                            var rootTemplate = Template;
                            while (rootTemplate.Template != null)
                                rootTemplate = rootTemplate.Template;
                            var genType = rootTemplate.GetRuntimeType();
                            if (genType == null) return null;
                            var args = ConsolidatedTemplateArguments;
                            var arguments = new Type[args.Count];
                            for (var i = 0; i < args.Count; i++) arguments[i] = args[i].GetRuntimeType();
                            return genType.MakeGenericType(arguments);
                        }
                        catch
                        {
                            //TODO: add error to metadata import errors if type is imported
                            return null;
                        }
#endif
                    if (DeclaringType != null)
                    {
                        var dt = DeclaringType.GetRuntimeType();
                        if (dt != null)
                        {
                            var flags = BindingFlags.DeclaredOnly;
                            if (IsPublic) flags |= BindingFlags.Public;
                            else flags |= BindingFlags.NonPublic;
                            runtimeType = dt.GetNestedType(Name.ToString(), flags);
                        }
                    }
                    else if (DeclaringModule != null && DeclaringModule.IsNormalized &&
                             DeclaringModule.ContainingAssembly != null)
                    {
                        var runtimeAssembly = DeclaringModule.ContainingAssembly.GetRuntimeAssembly();
                        if (runtimeAssembly != null)
                            runtimeType = runtimeAssembly.GetType(FullName, false);
                    }
                }

            return runtimeType;
        }
#endif
        public static TypeFlags GetVisibilityIntersection(TypeFlags vis1, TypeFlags vis2)
        {
            switch (vis2)
            {
                case TypeFlags.Public:
                case TypeFlags.NestedPublic:
                    return vis1;
                case TypeFlags.NotPublic:
                case TypeFlags.NestedAssembly:
                    switch (vis1)
                    {
                        case TypeFlags.Public:
                            return vis2;
                        case TypeFlags.NestedPublic:
                        case TypeFlags.NestedFamORAssem:
                            return TypeFlags.NestedAssembly;
                        case TypeFlags.NestedFamily:
                            return TypeFlags.NestedFamANDAssem;
                        default:
                            return vis1;
                    }
                case TypeFlags.NestedFamANDAssem:
                    switch (vis1)
                    {
                        case TypeFlags.Public:
                        case TypeFlags.NestedPublic:
                        case TypeFlags.NestedFamORAssem:
                        case TypeFlags.NestedFamily:
                            return TypeFlags.NestedFamANDAssem;
                        default:
                            return vis1;
                    }
                case TypeFlags.NestedFamORAssem:
                    switch (vis1)
                    {
                        case TypeFlags.Public:
                        case TypeFlags.NestedPublic:
                            return TypeFlags.NestedFamORAssem;
                        default:
                            return vis1;
                    }
                case TypeFlags.NestedFamily:
                    switch (vis1)
                    {
                        case TypeFlags.Public:
                        case TypeFlags.NestedPublic:
                        case TypeFlags.NestedFamORAssem:
                            return TypeFlags.NestedFamily;
                        case TypeFlags.NestedAssembly:
                            return TypeFlags.NestedFamANDAssem;
                        default:
                            return vis1;
                    }
                default:
                    return TypeFlags.NestedPrivate;
            }
        }

        private TrivialHashtable explicitInterfaceImplementations;

        public bool ImplementsExplicitly(Method method)
        {
            if (method == null) return false;
            var explicitInterfaceImplementations = this.explicitInterfaceImplementations;
            if (explicitInterfaceImplementations == null)
            {
                var members = Members;
                lock (this)
                {
                    if ((explicitInterfaceImplementations = this.explicitInterfaceImplementations) == null)
                    {
                        explicitInterfaceImplementations =
                            this.explicitInterfaceImplementations = new TrivialHashtable();
                        for (int i = 0, n = members.Count; i < n; i++)
                        {
                            var m = members[i] as Method;
                            if (m == null) continue;
                            var implementedInterfaceMethods = m.ImplementedInterfaceMethods;
                            if (implementedInterfaceMethods != null)
                                for (int j = 0, k = implementedInterfaceMethods.Count; j < k; j++)
                                {
                                    var im = implementedInterfaceMethods[j];
                                    if (im == null) continue;
                                    explicitInterfaceImplementations[im.UniqueKey] = m;
                                }
                        }
                    }
                }
            }

            return explicitInterfaceImplementations[method.UniqueKey] != null;
        }

        public Method ExplicitImplementation(Method method)
        {
            if (ImplementsExplicitly(method)) return (Method)explicitInterfaceImplementations[method.UniqueKey];
            return null;
        }

#if !MinimalReader
        internal bool ImplementsMethod(Method meth, bool checkPublic)
        {
            return GetImplementingMethod(meth, checkPublic) != null;
        }

        public Method GetImplementingMethod(Method meth, bool checkPublic)
        {
            if (meth == null) return null;
            var mems = GetMembersNamed(meth.Name);
            for (int j = 0, m = mems == null ? 0 : mems.Count; j < m; j++)
            {
                var locM = mems[j] as Method;
                if (locM == null || !locM.IsVirtual || (checkPublic && !locM.IsPublic)) continue;
                if ((locM.ReturnType != meth.ReturnType && !(locM.ReturnType != null &&
                                                             locM.ReturnType
                                                                 .IsStructurallyEquivalentTo(meth.ReturnType))) ||
                    !locM.ParametersMatchStructurally(meth.Parameters)) continue;
                return locM;
            }

            if (checkPublic && BaseType != null)
                return BaseType.GetImplementingMethod(meth, true);
            return null;
        }
#endif
        /// <summary>
        ///     Returns true if the CLR CTS allows a value of this type may be assigned to a variable of the target type (possibly
        ///     after boxing),
        ///     either because the target type is the same or a base type, or because the target type is an interface implemented
        ///     by this type or the implementor of this type,
        ///     or because this type and the target type are zero based single dimensional arrays with assignment compatible
        ///     reference element types
        /// </summary>
        public virtual bool IsAssignableTo(TypeNode targetType, Func<TypeNode, TypeNode> targetSubstitution = null)
        {
            if (this == CoreSystemTypes.Void) return false;
            if (targetType == this) return true;
            if (targetSubstitution != null && targetSubstitution(targetType) == this) return true;
            if (this == CoreSystemTypes.Object) return false;
            if (targetType == CoreSystemTypes.Object || IsStructurallyEquivalentTo(targetType, targetSubstitution) ||
                (BaseType != null && BaseType.IsAssignableTo(targetType, targetSubstitution)))
                return true;
            // if generic we need to check variance of parameters.
            if (Template != null && targetType.Template == Template)
            {
                var tpars1 = Template.ConsolidatedTemplateParameters;
                var targs1 = ConsolidatedTemplateArguments;
                var targs2 = targetType.ConsolidatedTemplateArguments;
                if (targs1.Count != targs2.Count) goto tryMore;
                for (var i = 0; i < targs1.Count; i++)
                {
                    var tp = tpars1[i] as ITypeParameter;
                    if (tp == null) goto tryMore;
                    if (tp.IsCovariant && !(targs1[i] is Struct))
                    {
                        if (!targs1[i].IsAssignableTo(targs2[i], targetSubstitution)) goto tryMore;
                    }
                    else if (tp.IsContravariant && !(targs2[i] is Struct))
                    {
                        if (!targs2[i].IsAssignableTo(targs1[i]))
                            goto tryMore; // lost substitution. Need to have substitution on the left too.
                    }
                    else
                    {
                        if (!targs1[i].IsStructurallyEquivalentTo(targs2[i], targetSubstitution)) goto tryMore;
                    }
                }

                return true;
            }

            tryMore:
            if (BaseType != null && ConsolidatedTemplateParameters != null && BaseType.Template != null &&
                BaseType.Template.IsAssignableTo(targetType))
                return
                    true; //When seeing if one template is assignable to another, be sure to strip off template instances along the inheritance chain
            var interfaces = Interfaces;
            if (interfaces == null) return false;
            for (int i = 0, n = interfaces.Count; i < n; i++)
            {
                var iface = interfaces[i];
                if (iface == null) continue;
                if (iface.IsAssignableTo(targetType, targetSubstitution)) return true;
                if (iface.Template != null && ConsolidatedTemplateParameters != null &&
                    iface.Template.IsAssignableTo(targetType))
                    return
                        true; //When seeing if one template is assignable to another, be sure to strip off template instances along the inheritance chain
            }

            return false;
        }

        /// <summary>
        ///     Returns true if this type is assignable to some instance of the given template.
        /// </summary>
        public virtual bool IsAssignableToInstanceOf(TypeNode targetTemplate)
        {
            if (this == CoreSystemTypes.Void || targetTemplate == null) return false;
            if (targetTemplate.IsStructurallyEquivalentTo(Template == null ? this : Template) ||
                (BaseType != null && (BaseType.IsAssignableToInstanceOf(targetTemplate) ||
                                      (BaseType.Template != null &&
                                       BaseType.Template.IsAssignableToInstanceOf(targetTemplate))))) return true;
            var interfaces = Interfaces;
            if (interfaces == null) return false;
            for (int i = 0, n = interfaces.Count; i < n; i++)
            {
                var iface = interfaces[i];
                if (iface == null) continue;
                if (iface.IsAssignableToInstanceOf(targetTemplate)) return true;
            }

            return false;
        }

        /// <summary>
        ///     Returns true if this type is assignable to some instance of the given template.
        /// </summary>
        public virtual bool IsAssignableToInstanceOf(TypeNode targetTemplate, out TypeNodeList templateArguments)
        {
            templateArguments = null;
            if (this == CoreSystemTypes.Void || targetTemplate == null) return false;
            if (targetTemplate == Template)
            {
                templateArguments = TemplateArguments;
                return true;
            }

            if (this != CoreSystemTypes.Object && BaseType != null &&
                BaseType.IsAssignableToInstanceOf(targetTemplate, out templateArguments)) return true;
            var interfaces = Interfaces;
            if (interfaces == null) return false;
            for (int i = 0, n = interfaces.Count; i < n; i++)
            {
                var iface = interfaces[i];
                if (iface == null) continue;
                if (iface.IsAssignableToInstanceOf(targetTemplate, out templateArguments)) return true;
            }

            return false;
        }

        /// <summary>
        ///     Returns true if otherType is the base class of this type or if the base class of this type is derived from
        ///     otherType.
        /// </summary>
        public virtual bool IsDerivedFrom(TypeNode otherType)
        {
            if (otherType == null) return false;
            var baseType = BaseType;
            while (baseType != null)
            {
                if (baseType == otherType) return true;
                baseType = baseType.BaseType;
            }

            return false;
        }
#if !MinimalReader
        //  Not thread safe code...
        private bool isCheckingInheritedFrom;
        public virtual bool IsInheritedFrom(TypeNode otherType)
        {
            if (otherType == null) return false;
            if (this == otherType) return true;
            var result = false;
            if (isCheckingInheritedFrom)
                goto done;
            isCheckingInheritedFrom = true;
            if (Template != null)
            {
                result = Template.IsInheritedFrom(otherType);
                goto done;
            }

            if (otherType.Template != null) otherType = otherType.Template;
            var baseType = BaseType;
            if (baseType != null && baseType.IsInheritedFrom(otherType))
            {
                result = true;
                goto done;
            }

            var interfaces = Interfaces;
            if (interfaces == null) goto done;
            for (int i = 0, n = interfaces.Count; i < n; i++)
            {
                var iface = interfaces[i];
                if (iface == null) continue;
                if (iface.IsInheritedFrom(otherType))
                {
                    result = true;
                    goto done;
                }
            }

            done:
            isCheckingInheritedFrom = false;
            return result;
        }
#endif
        public virtual bool IsStructurallyEquivalentTo(TypeNode type, Func<TypeNode, TypeNode> typeSubstitution = null)
        {
            if (null == (object)type) return false;
            if (this == type) return true;
            if (typeSubstitution != null && this == typeSubstitution(type)) return true;
            if (Template == (object)null || type.Template == (object)null)
            {
                if (this == (object)type.Template || Template == (object)type) return true;
                var thisName = Template == null ? Name : Template.Name;
                var typeName = type.Template == null ? type.Name : type.Template.Name;
                if (thisName == null || typeName == null || thisName.UniqueIdKey != typeName.UniqueIdKey) return false;
                if (NodeType != type.NodeType) return false;
                if (DeclaringType == null || type.DeclaringType == null) return false;
            }

            if (TemplateArguments == null || type.TemplateArguments == null)
            {
                if (DeclaringType != null && (TemplateArguments == null || TemplateArguments.Count == 0) &&
                    (type.TemplateArguments == null || type.TemplateArguments.Count == 0))
                    return DeclaringType.IsStructurallyEquivalentTo(type.DeclaringType);
                return false;
            }

            var n = TemplateArguments.Count;
            if (n != type.TemplateArguments.Count) return false;
            if (Template != type.Template && !Template.IsStructurallyEquivalentTo(type.Template)) return false;
            for (var i = 0; i < n; i++)
            {
                var ta1 = TemplateArguments[i];
                var ta2 = type.TemplateArguments[i];
                if (null == (object)ta1 || null == (object)ta2) return false;
                if (ta1 == ta2) continue;
                if (!ta1.IsStructurallyEquivalentTo(ta2, typeSubstitution)) return false;
            }

            if (DeclaringType != null)
                return DeclaringType.IsStructurallyEquivalentTo(type.DeclaringType);
            return true;
        }

        public virtual bool IsStructurallyEquivalentList(TypeNodeList list1, TypeNodeList list2,
            Func<TypeNode, TypeNode> typeSubstitution = null)
        {
            if (list1 == null) return list2 == null;
            if (list2 == null) return false;
            var n = list1.Count;
            if (list2.Count != n) return false;
            for (var i = 0; i < n; i++)
            {
                var t1 = list1[i];
                var t2 = list2[i];
                if (null == (object)t1 || null == (object)t2) return false;
                if (t1 == t2) continue;
                if (!t1.IsStructurallyEquivalentTo(t2, typeSubstitution)) return false;
            }

            return true;
        }

        public static TypeNode StripModifiers(TypeNode type)
            //^ ensures t != null ==> result != null;
        {
            for (var tmod = type as TypeModifier; tmod != null; tmod = type as TypeModifier)
                type = tmod.ModifiedType;
            // Don't strip under pointers or refs. We only strip top-level modifiers.
            return type;
        }
#if !MinimalReader
        public static TypeNode DeepStripModifiers(TypeNode type)
            //^ ensures type != null ==> result != null;
        {
            // strip off any outer type modifiers
            for (var tmod = type as TypeModifier; tmod != null; tmod = type as TypeModifier)
                type = tmod.ModifiedType;
            // For arrays and references, strip the inner type and then reconstruct the array or reference
            var ar = type as ArrayType;
            if (ar != null)
            {
                var t = DeepStripModifiers(ar.ElementType);
                return t.GetArrayType(1);
            }

            var rt = type as Reference;
            if (rt != null)
            {
                var t = DeepStripModifiers(rt.ElementType);
                return t.GetReferenceType();
            }

            return type;
        }

        /// <summary>
        ///     Strip the given modifier from the type, modulo substructures that are instantiated with respect
        ///     to the given template type. In other words, travers type and templateType in parallel, stripping common
        ///     non-null modifiers, but stop when reaching a type variable in the template type.
        ///     <param name="type">Type to be stripped</param>
        ///     <param name="modifiers">Modifiers to strip off</param>
        ///     <param name="templateType">
        ///         Template bounding the stripping of type. Passing null for the templateType performs a
        ///         full DeepStrip
        ///     </param>
        /// </summary>
        public static TypeNode DeepStripModifiers(TypeNode type, TypeNode templateType, params TypeNode[] modifiers)
        {
            if (templateType == null) return DeepStripModifiers(type, modifiers);
            if (templateType is ITypeParameter) return type;
            // strip off inner modifiers then outer type modifier if it matches
            var optmod = type as OptionalModifier;
            if (optmod != null)
            {
                var optmodtemplate = (OptionalModifier)templateType; // must be in sync
                var t = DeepStripModifiers(optmod.ModifiedType, optmodtemplate.ModifiedType, modifiers);
                for (var i = 0; i < modifiers.Length; ++i)
                    if (optmod.Modifier == modifiers[i]) // strip it
                        return t;
                return OptionalModifier.For(optmod.Modifier, t);
            }

            var reqmod = type as RequiredModifier;
            if (reqmod != null)
            {
                var reqmodtemplate = (RequiredModifier)templateType; // must be in sync
                var t = DeepStripModifiers(reqmod.ModifiedType, reqmodtemplate.ModifiedType, modifiers);
                for (var i = 0; i < modifiers.Length; ++i)
                    if (reqmod.Modifier == modifiers[i]) // strip it
                        return t;
                return RequiredModifier.For(reqmod.Modifier, t);
            }

            // For arrays and references, strip the inner type and then reconstruct the array or reference
            var ar = type as ArrayType;
            if (ar != null)
            {
                var artemplate = (ArrayType)templateType;
                var t = DeepStripModifiers(ar.ElementType, artemplate.ElementType, modifiers);
                return t.GetArrayType(1);
            }

            var rt = type as Reference;
            if (rt != null)
            {
                var rttemplate = (Reference)templateType;
                var t = DeepStripModifiers(rt.ElementType, rttemplate.ElementType, modifiers);
                return t.GetReferenceType();
            }

            // strip template arguments
            if (type.Template != null && type.TemplateArguments != null && type.TemplateArguments.Count > 0)
            {
                var strippedArgs = new TypeNodeList(type.TemplateArguments.Count);
                for (var i = 0; i < type.TemplateArguments.Count; i++)
                {
                    //FIX: bug introduced by checkin 16494 
                    //templateType may have type parameters in either the TemplateArguments position or the templateParameters position.
                    //This may indicate an inconsistency in the template instantiation representation elsewhere.
                    var templateTypeArgs = templateType.TemplateArguments != null
                        ? templateType.TemplateArguments
                        : templateType.TemplateParameters;
                    strippedArgs.Add(DeepStripModifiers(type.TemplateArguments[i], templateTypeArgs[i], modifiers));
                }

                return type.Template.GetTemplateInstance(type, strippedArgs);
            }

            return type;
        }

        public static TypeNode DeepStripModifiers(TypeNode type, params TypeNode[] modifiers)
        {
            // strip off inner modifiers then outer type modifier if it matches
            var optmod = type as OptionalModifier;
            if (optmod != null)
            {
                var t = DeepStripModifiers(optmod.ModifiedType, modifiers);
                for (var i = 0; i < modifiers.Length; ++i)
                    if (optmod.Modifier == modifiers[i]) // strip it
                        return t;
                return OptionalModifier.For(optmod.Modifier, t);
            }

            var reqmod = type as RequiredModifier;
            if (reqmod != null)
            {
                var t = DeepStripModifiers(reqmod.ModifiedType, modifiers);
                for (var i = 0; i < modifiers.Length; ++i)
                    if (reqmod.Modifier == modifiers[i]) // strip it
                        return t;
                return RequiredModifier.For(reqmod.Modifier, t);
            }

            // For arrays and references, strip the inner type and then reconstruct the array or reference
            var ar = type as ArrayType;
            if (ar != null)
            {
                var t = DeepStripModifiers(ar.ElementType, modifiers);
                return t.GetArrayType(1);
            }

            var rt = type as Reference;
            if (rt != null)
            {
                var t = DeepStripModifiers(rt.ElementType, modifiers);
                return t.GetReferenceType();
            }

            // strip template arguments
            if (type.Template != null && type.TemplateArguments != null && type.TemplateArguments.Count > 0)
            {
                var strippedArgs = new TypeNodeList(type.TemplateArguments.Count);
                for (var i = 0; i < type.TemplateArguments.Count; i++)
                    strippedArgs.Add(DeepStripModifiers(type.TemplateArguments[i], modifiers));
                return type.Template.GetTemplateInstance(type, strippedArgs);
            }

            return type;
        }
#endif
        public static bool HasModifier(TypeNode type, TypeNode modifier)
        {
            // Don't look under pointers or refs.
            var tmod = type as TypeModifier;
            if (tmod != null)
            {
                if (tmod.Modifier == modifier) return true;
                return HasModifier(tmod.ModifiedType, modifier);
            }

            return false;
        }

        public static TypeNode StripModifier(TypeNode type, TypeNode modifier)
        {
            // Don't strip under pointers or refs. We only strip top-level modifiers
            var tmod = type as TypeModifier;
            if (tmod != null)
            {
                var et = StripModifier(tmod.ModifiedType, modifier);
                //^ assert et != null;
                if (tmod.Modifier == modifier) return et;
                if (et == tmod.ModifiedType) return tmod;
                if (tmod is OptionalModifier) return OptionalModifier.For(tmod.Modifier, et);
                return RequiredModifier.For(tmod.Modifier, et);
            }

            return type;
        }

        /// <summary>
        ///     Needed whenever we change the id of an existing member
        /// </summary>
#if !MinimalReader
        public
#else
    internal
#endif
            virtual void ClearMemberTable()
        {
            lock (this)
            {
                memberTable = null;
                memberCount = 0;
            }
        }

        protected virtual void UpdateMemberTable(int range)
            //^ ensures this.memberTable != null;
        {
            var thisMembers = Members;
            lock (this)
            {
                if (memberTable == null) memberTable = new TrivialHashtable(32);
                for (var i = memberCount; i < range; i++)
                {
                    var mem = thisMembers[i];
                    if (mem == null || mem.Name == null) continue;
                    var members = (MemberList)memberTable[mem.Name.UniqueIdKey];
                    if (members == null) memberTable[mem.Name.UniqueIdKey] = members = new MemberList(2);
                    members.Add(mem);
                }

                memberCount = range;
                constructors = null;
            }
        }

        protected static MemberList WeedOutNonSpecialMethods(MemberList members, MethodFlags mask)
        {
            if (members == null) return null;
            var membersOK = true;
            for (int i = 0, n = members.Count; i < n; i++)
            {
                var m = members[i] as Method;
                if (m == null || (m.Flags & mask) == 0)
                {
                    membersOK = false;
                    break;
                }
            }

            if (membersOK) return members;
            var newMembers = new MemberList();
            for (int i = 0, n = members.Count; i < n; i++)
            {
                var m = members[i] as Method;
                if (m == null || (m.Flags & mask) == 0) continue;
                newMembers.Add(m);
            }

            return newMembers;
        }
#if !NoXml
        public override void WriteDocumentation(XmlTextWriter xwriter)
        {
            base.WriteDocumentation(xwriter);
            var members = Members;
            for (int i = 0, n = members == null ? 0 : members.Count; i < n; i++)
            {
                var mem = members[i];
                if (mem == null) continue;
                mem.WriteDocumentation(xwriter);
            }
        }
#endif
#if ExtendedRuntime
    public TypeNode StripOptionalModifiers(out bool nonNull){
      TypeNode t = this;
      nonNull = false;
      for(;;){
        OptionalModifier m = t as OptionalModifier;
        if (m == null)
          break;
        if (m.Modifier == SystemTypes.NonNullType)
          nonNull = true;
        t = m.ModifiedType;
      }
      return t;
    }
    public bool IsObjectReferenceType{
      get{
        bool nonNull;
        TypeNode t = this.StripOptionalModifiers(out nonNull);
        return t is Class || t is Interface || t is ArrayType || t is DelegateNode;
      }
    }
#endif
        public override string ToString()
        {
#if !FxCop
            return GetFullUnmangledNameWithTypeParameters();
#else
      return base.ToString() + ":" + this.GetFullUnmangledNameWithTypeParameters();
#endif
        }
#if FxCop
    internal override void GetName(MemberFormat options, StringBuilder name)
    {
      GetName(options.Type, name);
    }
    internal virtual void GetName(TypeFormat options, StringBuilder name)
    {
      if (options.TypeName != TypeNameFormat.None)
      {
        if (this.DeclaringType != null && options.TypeName != TypeNameFormat.InnermostNested)
        {
          this.DeclaringType.GetName(options, name);
          name.Append('+');
        }
        else if (options.TypeName == TypeNameFormat.FullyQualified && this.Namespace.Name.Length > 0)
        {
          name.Append(this.Namespace.Name);
          name.Append('.');
        }
        string shortName = this.Name.Name;
        int mangleChar = shortName.IndexOf(TargetPlatform.GenericTypeNamesMangleChar);
        if (mangleChar != -1)
          shortName = shortName.Substring(0, mangleChar);
        name.Append(shortName);
      }
      TypeNodeList templateParameters = this.TemplateParameters;
      if (this.Template != null) templateParameters = this.TemplateArguments;
      if (templateParameters != null)
      {
        if (options.ShowGenericTypeArity)
        {
          name.Append(TargetPlatform.GenericTypeNamesMangleChar);
          int parametersCount = templateParameters.Count;
          name.Append(Convert.ToString(parametersCount, CultureInfo.InvariantCulture));
        }
        if (options.ShowGenericTypeParameterNames)
        {
          name.Append('<');
          int parametersCount = templateParameters.Count;
          for (int i = 0; i < parametersCount; ++i)
          {
            if (i > 0)
            {
              name.Append(',');
              if (options.InsertSpacesBetweenTypeParameters) name.Append(' ');
            }
            templateParameters[i].GetName(options, name);
          }
          name.Append('>');
        }
      }
    }
#endif
    }
#if FxCop
  public class ClassNode : TypeNode{
#else
    public class Class : TypeNode
    {
#endif
        internal static readonly Class DoesNotExist = new Class();
        internal static readonly Class Dummy = new Class();
        internal Class baseClass;
#if !MinimalReader
        public Class BaseClassExpression;
        public bool IsAbstractSealedContainerForStatics;
#endif
#if FxCop
    public ClassNode()
      : base(NodeType.Class){
    }
    public ClassNode(NestedTypeProvider provideNestedTypes, TypeAttributeProvider provideAttributes, TypeMemberProvider provideMembers, object handle)
      : base(NodeType.Class, provideNestedTypes, provideAttributes, provideMembers, handle){
    }
#else
        public Class()
            : base(NodeType.Class)
        {
        }

        public Class(NestedTypeProvider provideNestedTypes, TypeAttributeProvider provideAttributes,
            TypeMemberProvider provideMembers, object handle)
            : base(NodeType.Class, provideNestedTypes, provideAttributes, provideMembers, handle)
        {
        }
#endif
#if !MinimalReader
        public Class(Module declaringModule, TypeNode declaringType, AttributeList attributes, TypeFlags flags,
            Identifier Namespace, Identifier name, Class baseClass, InterfaceList interfaces, MemberList members)
            : base(declaringModule, declaringType, attributes, flags, Namespace, name, interfaces, members,
                NodeType.Class)
        {
            this.baseClass = baseClass;
        }
#endif
        /// <summary>
        ///     The class from which this class has been derived. Null if this class is System.Object.
        /// </summary>
        public virtual Class BaseClass
        {
            get
            {
                // for completing duplication etc, touch interfaces
                var _ = Interfaces;

                return baseClass;
            }
            set { baseClass = value; }
        }
#if !MinimalReader
        public override void GetAbstractMethods(MethodList /*!*/ result)
        {
            if (!IsAbstract) return;
            var candidates = new MethodList();
            if (BaseClass != null)
            {
                BaseClass.GetAbstractMethods(candidates);
                for (int i = 0, n = candidates.Count; i < n; i++)
                {
                    var meth = candidates[i];
                    if (!ImplementsMethod(meth, false)) result.Add(meth);
                }
            }

            //Add any abstract methods declared inside this class
            var members = Members;
            for (int i = 0, n = members.Count; i < n; i++)
            {
                var meth = members[i] as Method;
                if (meth == null) continue;
                if (meth.IsAbstract) result.Add(meth);
            }

            //For each interface, get abstract methods and keep those that are not implemented by this class or a base class
            var interfaces = Interfaces;
            if (interfaces != null)
                for (int i = 0, n = interfaces.Count; i < n; i++)
                {
                    var iface = interfaces[i];
                    if (iface == null) continue;
                    var imembers = iface.Members;
                    if (imembers == null) continue;
                    for (int j = 0, m = imembers.Count; j < m; j++)
                    {
                        var meth = imembers[j] as Method;
                        if (meth == null) continue;
                        if (ImplementsExplicitly(meth)) continue;
                        if (ImplementsMethod(meth, true)) continue;
                        if (AlreadyInList(result, meth)) continue;
                        result.Add(meth);
                    }
                }
        }

        protected static bool AlreadyInList(MethodList list, Method method)
        {
            if (list == null) return false;
            for (int i = 0, n = list.Count; i < n; i++)
                if (list[i] == method)
                    return true;
            return false;
        }
#endif
#if ExtendedRuntime
    public bool IsGuarded{
      get{
        Field f = this.GetField(Identifier.For("SpecSharp::frameGuard"));
        return f != null;
      }
    }
#endif
    }
#if !MinimalReader && !CodeContracts
  public class ClosureClass : Class{
    public ClosureClass(){
    }    
  } 
  /// <summary>
  /// Does not model a real type, but leverages the symbol table methods of Class. In other words, this is implementation inheritance, not an ISA relationship.
  /// </summary>
  //TODO: use delegation rather than inheritance to achieve this
  public class Scope : Class{
    public Scope(){
    }
    public Scope(Scope outerScope){
      this.OuterScope = outerScope;
    }
    protected Scope outerScope;
    public SourceContext LexicalSourceExtent;
    public Scope OuterScope{
      get{
        if (this.outerScope == null)
          this.outerScope = (Scope)this.baseClass;
        return this.outerScope;
      }
      set{
        this.baseClass = this.outerScope = value;
      }
    }
    public virtual TypeNode GetType(Identifier typeName){
      return this.GetNestedType(typeName);
    }
  }
  public class TypeScope : Scope{
    public TypeNode Type;
    public TypeScope(){}
    public TypeScope(Scope parentScope, TypeNode/*!*/ type) {
      this.baseClass = parentScope;
      this.DeclaringModule = type.DeclaringModule;
      this.Type = type;
      if (type != null && type.PartiallyDefines != null) this.Type = type.PartiallyDefines;
      this.templateParameters = type.TemplateParameters;
      if (type != null)
        this.LexicalSourceExtent = type.SourceContext;
    }
    public override MemberList/*!*/ GetMembersNamed(Identifier name) {
      TypeNode t = this.Type;
      MemberList result = null;
      while (t != null){
        result = t.GetMembersNamed(name);
        if (result.Count > 0) return result;
        t = t.BaseType;
      }
      if (result != null) return result;
      return new MemberList(0);
    }
    public override MemberList Members{
      get{
        return this.Type.Members;
      }
      set{
        base.Members = value;
      }
    }
  }
  public class MethodScope : Scope{
    protected Class closureClass;
    public virtual Class ClosureClass{
      get{
        //if (this.DeclaringMethod == null) return null;
        Class c = this.closureClass;
        if (c == null){
          c = this.closureClass = new ClosureClass();
          c.Name = Identifier.For("closure:"+this.UniqueKey);
          c.BaseClass = CoreSystemTypes.Object;
          Class bclass = this.BaseClass;
          c.DeclaringModule = bclass.DeclaringModule;
          TypeScope tscope = bclass as TypeScope;
          if (tscope != null)
            c.DeclaringType = tscope.Type;
          else{
            MethodScope mscope = bclass as MethodScope;
            if (mscope != null)
              c.DeclaringType = mscope.ClosureClass;
            else
              c.DeclaringType = ((BlockScope)bclass).ClosureClass;
          }
          c.IsGeneric = c.DeclaringType.IsGeneric || this.DeclaringMethod.IsGeneric;
          c.TemplateParameters = this.CopyMethodTemplateParameters(c.DeclaringModule, c.DeclaringType);
          c.Flags = TypeFlags.NestedPrivate|TypeFlags.SpecialName|TypeFlags.Sealed;
          c.Interfaces = new InterfaceList(0);
          if (this.ThisType != null){
            Field f =
 new Field(c, null, FieldFlags.CompilerControlled | FieldFlags.SpecialName, StandardIds.ThisValue, this.ThisType, null);
            this.ThisField = f;
            c.Members.Add(f);
          }
        }
        return c;
      }
    }
    private TypeNodeList CopyMethodTemplateParameters(Module/*!*/ module, TypeNode/*!*/ type) 
      //^ requires this.DeclaringMethod != null;
    {
      TypeNodeList methTemplParams = this.DeclaringMethod.TemplateParameters;
      if (methTemplParams == null || methTemplParams.Count == 0) return null;
      this.tpDup = new TemplateParameterDuplicator(module, type);
      return this.tpDup.VisitTypeParameterList(methTemplParams);
    }
    private TemplateParameterDuplicator tpDup;
    private class TemplateParameterDuplicator : Duplicator{
      public TemplateParameterDuplicator(Module/*!*/ module, TypeNode/*!*/ type)
        : base(module, type){
      }

      public override TypeNode VisitTypeParameter(TypeNode typeParameter){
        if (typeParameter == null) return null;
        TypeNode result = (TypeNode)this.DuplicateFor[typeParameter.UniqueKey];
        if (result != null) return result;
        MethodTypeParameter mtp = typeParameter as MethodTypeParameter;
        if (mtp != null){
          TypeParameter tp = new TypeParameter();
          this.DuplicateFor[typeParameter.UniqueKey] = tp;
          tp.Name = mtp.Name;
          tp.Interfaces = this.VisitInterfaceReferenceList(mtp.Interfaces);
          tp.TypeParameterFlags = mtp.TypeParameterFlags;
          tp.DeclaringModule = mtp.DeclaringModule;
          tp.DeclaringMember = this.TargetType;
          result = tp;
        }else{
          MethodClassParameter mcp = typeParameter as MethodClassParameter;
          if (mcp != null){
            ClassParameter cp = new ClassParameter();
            this.DuplicateFor[typeParameter.UniqueKey] = cp;
            cp.Name = mcp.Name;
            cp.BaseClass = (Class)this.VisitTypeReference(mcp.BaseClass);
            cp.Interfaces = this.VisitInterfaceReferenceList(mcp.Interfaces);
            cp.TypeParameterFlags = mcp.TypeParameterFlags;
            cp.DeclaringModule = mcp.DeclaringModule;
            cp.DeclaringMember = this.TargetType;
            result = cp;
          }
        }
        if (result == null) return typeParameter;
        return result;
      }
      public override TypeNode VisitTypeReference(TypeNode type){
        TypeNode result = base.VisitTypeReference(type);
        if (result == type && (type is MethodClassParameter || type is MethodTypeParameter))
          return this.VisitTypeParameter(type);
        return result;
      }
    }
    public virtual Class ClosureClassTemplateInstance {
      get{
        if (this.closureClassTemplateInstance == null) {
          if (this.DeclaringMethod == null || !this.DeclaringMethod.IsGeneric)
            this.closureClassTemplateInstance = this.ClosureClass;
          else
            this.closureClassTemplateInstance =
 (Class)this.ClosureClass.GetTemplateInstance(this.DeclaringMethod.DeclaringType, this.DeclaringMethod.TemplateParameters);
        }
        return this.closureClassTemplateInstance;
      }
    }
    Class closureClassTemplateInstance;
    public TypeNode FixTypeReference(TypeNode type) {
      if (this.tpDup == null) return type;
      return this.tpDup.VisitTypeReference(type);
    }

    public virtual Boolean CapturedForClosure{
      get{
        return this.closureClass != null;
      }
    }
    public UsedNamespaceList UsedNamespaces;
    public Field ThisField;
    public TypeNode ThisType;
    public TypeNode ThisTypeInstance;
    public Method DeclaringMethod;
    public MethodScope(){}
    public MethodScope(Class/*!*/ parentScope, UsedNamespaceList usedNamespaces)
      : this(parentScope, usedNamespaces, null){
    }
    public MethodScope(Class/*!*/ parentScope, UsedNamespaceList usedNamespaces, Method method) {
      this.baseClass = parentScope;
      this.UsedNamespaces = usedNamespaces;
      this.DeclaringModule = parentScope.DeclaringModule;
      this.DeclaringMethod = method;
      if (method != null && (method.Flags & MethodFlags.Static) == 0)
        this.ThisType = this.ThisTypeInstance = method.DeclaringType;
      if (method != null)
        this.LexicalSourceExtent = method.SourceContext;
    }
  }
  public class BlockScope : Scope{
    public Block AssociatedBlock;
    public bool MembersArePinned;
    public virtual Class ClosureClass{
      get{
        BlockScope bscope = this.BaseClass as BlockScope;
        if (bscope != null) return bscope.ClosureClass;
        MethodScope mscope = this.BaseClass as MethodScope;
        if (mscope != null) return mscope.ClosureClass;
        return ((TypeScope)this.BaseClass).Type as Class;
      }
    }
    public virtual Boolean CapturedForClosure{
      get{
        BlockScope bscope = this.BaseClass as BlockScope;
        if (bscope != null) return bscope.CapturedForClosure;
        MethodScope mscope = this.BaseClass as MethodScope;
        if (mscope != null) return mscope.CapturedForClosure;
        return false;
      }
    }
    public BlockScope(){
    }
    public BlockScope(Scope/*!*/ parentScope, Block associatedBlock) {
      this.AssociatedBlock = associatedBlock;
      if (associatedBlock != null){
        associatedBlock.HasLocals = true; //TODO: set only if there really are locals
        associatedBlock.Scope = this;
      }
      this.baseClass = parentScope;
      this.DeclaringModule = parentScope.DeclaringModule;
      if (associatedBlock != null)
        this.LexicalSourceExtent = associatedBlock.SourceContext;
    }
  }
  public class AttributeScope : Scope{
    public AttributeNode AssociatedAttribute;
    public AttributeScope(Scope parentScope, AttributeNode associatedAttribute){
      this.AssociatedAttribute = associatedAttribute;
      this.baseClass = parentScope;
      if (associatedAttribute != null)
        this.LexicalSourceExtent = associatedAttribute.SourceContext;
    }
  }
  public class NamespaceScope : Scope{
    public Namespace AssociatedNamespace;
    public Module AssociatedModule;
    public TrivialHashtable AliasedType;
    public TrivialHashtable AliasedNamespace;
    protected TrivialHashtable/*!*/ aliasFor = new TrivialHashtable();
    protected TrivialHashtable/*!*/ typeFor = new TrivialHashtable();
    protected TrivialHashtable/*!*/ namespaceFor = new TrivialHashtable();
    protected TrivialHashtable/*!*/ nestedNamespaceFullName = new TrivialHashtable();
    protected readonly static AliasDefinition/*!*/ noSuchAlias = new AliasDefinition();

    public NamespaceScope(){
    }
    public NamespaceScope(Scope outerScope, Namespace associatedNamespace, Module associatedModule)
      : base(outerScope){
      //^ base;
      this.AssociatedNamespace = associatedNamespace;
      this.AssociatedModule = associatedModule;
      this.DeclaringModule = associatedModule; //TODO: make this go away
      if (associatedNamespace != null)
        this.LexicalSourceExtent = associatedNamespace.SourceContext;
    }
    public virtual AliasDefinition GetAliasFor(Identifier name){
      if (name == null || this.AssociatedNamespace == null || this.AssociatedModule == null || this.aliasFor == null){
        Debug.Assert(false); return null;
      }
      AliasDefinition alias = (AliasDefinition)this.aliasFor[name.UniqueIdKey];
      if (alias == noSuchAlias) return null;
      if (alias != null) return alias;
      //Check if there is an alias with this uri
      Scope scope = this;
      while (scope != null){
        NamespaceScope nsScope = scope as NamespaceScope;
        if (nsScope != null && nsScope.AssociatedNamespace != null){
          AliasDefinitionList aliases = nsScope.AssociatedNamespace.AliasDefinitions;
          if (aliases != null)
            for (int i = 0, n = aliases.Count; i < n; i++){
              AliasDefinition aliasDef = aliases[i];
              if (aliasDef == null || aliasDef.Alias == null) continue;
              if (aliasDef.Alias.UniqueIdKey == name.UniqueIdKey){alias = aliasDef; goto done;}
            }
        }
        scope = scope.OuterScope;
      }
      done:
        if (alias != null)
          this.aliasFor[name.UniqueIdKey] = alias;
        else
          this.aliasFor[name.UniqueIdKey] = noSuchAlias;
      return alias;
    }
    public virtual AliasDefinition GetConflictingAlias(Identifier name){
      if (name == null || this.typeFor == null || this.AssociatedNamespace == null || this.AssociatedModule == null){
        Debug.Assert(false); return null;
      }
      TypeNode type = this.AssociatedModule.GetType(this.AssociatedNamespace.FullNameId, name);
      if (type != null) {
        AliasDefinitionList aliases = this.AssociatedNamespace.AliasDefinitions;
        for (int i = 0, n = aliases == null ? 0 : aliases.Count; i < n; i++){
          //^ assert aliases != null;
          AliasDefinition aliasDef = aliases[i];
          if (aliasDef == null || aliasDef.Alias == null) continue;
          if (aliasDef.Alias.UniqueIdKey == name.UniqueIdKey) return aliasDef;
        }
      }
      Scope scope = this;
      while (scope != null) {
        NamespaceScope outerScope = scope.OuterScope as NamespaceScope;
        if (outerScope != null) return outerScope.GetConflictingAlias(name);
        scope = scope.OuterScope;
      }
      return null;
    }
    public virtual Identifier GetUriFor(Identifier name) {
      AliasDefinition aliasDef = this.GetAliasFor(name);
      if (aliasDef == null) return null;
      return aliasDef.AliasedUri;
    }
    public virtual Identifier GetNamespaceFullNameFor(Identifier name){
      if (name == null || this.AssociatedNamespace == null || this.AssociatedModule == null || this.nestedNamespaceFullName == null){
        Debug.Assert(false); return null;
      }
      Identifier fullName = (Identifier)this.nestedNamespaceFullName[name.UniqueIdKey];
      if (fullName == Identifier.Empty) return null;
      if (fullName != null) return fullName;
      //Check if there is an alias with this namespace
      AliasDefinition aliasDef = this.GetAliasFor(name);
      if (aliasDef != null && aliasDef.AliasedUri == null && aliasDef.AliasedType == null)
        return aliasDef.AliasedExpression as Identifier;
      //Check if module has a type with namespace equal to this namespace + name
      fullName = name;
      if (this.AssociatedNamespace.Name != null && this.AssociatedNamespace.Name.UniqueIdKey != Identifier.Empty.UniqueIdKey)
        fullName = Identifier.For(this.AssociatedNamespace.FullName+"."+name);
      if (this.AssociatedModule.IsValidNamespace(fullName)){
        this.namespaceFor[fullName.UniqueIdKey] = new TrivialHashtable();
        goto returnFullName;
      }
      // If an inner type shadows an outer namespace, don't return the namespace
      if (this.AssociatedModule.IsValidTypeName(this.AssociatedNamespace.Name, name)) { return null; }
      AssemblyReferenceList arefs = this.AssociatedModule.AssemblyReferences;
      for (int i = 0, n = arefs == null ? 0 : arefs.Count; i < n; i++){
        AssemblyReference ar = arefs[i];
        if (ar == null || ar.Assembly == null) continue;
        if (ar.Assembly.IsValidNamespace(fullName)) goto returnFullName;
        // If an inner type shadows an outer namespace, don't return the namespace
        if (ar.Assembly.IsValidTypeName(this.AssociatedNamespace.Name, name)) { return null; }
      }
      ModuleReferenceList mrefs = this.AssociatedModule.ModuleReferences;
      if (mrefs != null)
        for (int i = 0, n = mrefs.Count; i < n; i++){
          ModuleReference mr = mrefs[i];
          if (mr == null || mr.Module == null) continue;
          if (mr.Module.IsValidNamespace(fullName)) goto returnFullName;
          // If an inner type shadows an outer namespace, don't return the namespace
          if (mr.Module.IsValidTypeName(this.AssociatedNamespace.Name, name)) { return null; }
        }
      Scope scope = this.OuterScope;
      while (scope != null && !(scope is NamespaceScope)) scope = scope.OuterScope;
      if (scope != null) return ((NamespaceScope)scope).GetNamespaceFullNameFor(name);
      return null;
    returnFullName:
      this.nestedNamespaceFullName[name.UniqueIdKey] = fullName;
      return fullName;
    }
    /// <summary>
    /// Search this namespace for a type with this name nested in the given namespace. Also considers used name spaces.
    /// If more than one type is found, a list is returned in duplicates.
    /// </summary>
    public virtual TypeNode GetType(Identifier Namespace, Identifier name, out TypeNodeList duplicates){
      duplicates = null;
      if (Namespace == null || name == null || this.AssociatedNamespace == null || this.AssociatedModule == null){
        Debug.Assert(false); return null;
      }
      if (this.namespaceFor == null){
        Debug.Assert(false);
        this.namespaceFor = new TrivialHashtable();
      }
      TrivialHashtable typeFor = (TrivialHashtable)this.namespaceFor[Namespace.UniqueIdKey];
      if (typeFor == null) this.namespaceFor[Namespace.UniqueIdKey] = typeFor = new TrivialHashtable();
      TypeNode result = (TypeNode)typeFor[name.UniqueIdKey];
      if (result == Class.DoesNotExist) return null;
      if (result != null) return result;
      //If the associated module declares a type with the given name in a nested namespace, it wins
      Scope scope = this;
      while (scope != null){
        NamespaceScope nsScope = scope as NamespaceScope;
        if (nsScope != null && nsScope.AssociatedNamespace != null){
          Identifier nestedNamespace = Namespace;
          if (nsScope.AssociatedNamespace.FullNameId != null && nsScope.AssociatedNamespace.FullNameId.UniqueIdKey != Identifier.Empty.UniqueIdKey)
            nestedNamespace = Identifier.For(nsScope.AssociatedNamespace.FullName+"."+Namespace);
          result = this.AssociatedModule.GetType(nestedNamespace, name);
          if (result != null) break;
        }
        scope = scope.OuterScope;
      }
      if (result == null){
        //Now get into situations where there might be duplicates.
        duplicates = new TypeNodeList();
        //Check the used namespaces of this and outer namespace scopes
        TrivialHashtable alreadyUsed = new TrivialHashtable();
        scope = this;
        while (scope != null){
          NamespaceScope nsScope = scope as NamespaceScope;
          if (nsScope != null && nsScope.AssociatedNamespace != null){
            UsedNamespaceList usedNamespaces = nsScope.AssociatedNamespace.UsedNamespaces;
            int n = usedNamespaces == null ? 0 : usedNamespaces.Count;
            if (usedNamespaces != null)
              for (int i = 0; i < n; i++){
                UsedNamespace usedNs = usedNamespaces[i];
                if (usedNs == null || usedNs.Namespace == null) continue;
                int key = usedNs.Namespace.UniqueIdKey;
                if (alreadyUsed[key] != null) continue;
                alreadyUsed[key] = usedNs.Namespace;
                Identifier usedNestedNamespace = Identifier.For(usedNs.Namespace+"."+Namespace);
                result = this.AssociatedModule.GetType(usedNestedNamespace, name);
                if (result != null) duplicates.Add(result);
              }
          }
          scope = scope.OuterScope;
        }
        if (duplicates.Count > 0) result = duplicates[0];
      }
      if (result == null){
        //The associated module does not have a type by this name, so check its referenced modules and assemblies
        int numDups = 0;
        //Check this namespace and outer namespaces
        scope = this;
        while (scope != null && result == null){
          NamespaceScope nsScope = scope as NamespaceScope;
          if (nsScope != null && nsScope.AssociatedNamespace != null){
            Identifier nestedNamespace = Namespace;
            if (nsScope.AssociatedNamespace.FullNameId != null && nsScope.AssociatedNamespace.FullNameId.UniqueIdKey != Identifier.Empty.UniqueIdKey)
              nestedNamespace = Identifier.For(nsScope.AssociatedNamespace.FullName+"."+Namespace);
            nsScope.GetReferencedTypes(nestedNamespace, name, duplicates);
            numDups = duplicates.Count;
            for (int i = numDups-1; i >= 0; i--){
              TypeNode dup = duplicates[i];
              if (dup == null || !dup.IsPublic) numDups--;
              result = dup;
            }
          }
          scope = scope.OuterScope;
        }
        if (numDups == 0){
          if (duplicates.Count > 0) duplicates = new TypeNodeList();
          //Check the used namespaces of this and outer namespace scopes
          TrivialHashtable alreadyUsed = new TrivialHashtable();
          scope = this;
          while (scope != null){
            NamespaceScope nsScope = scope as NamespaceScope;
            if (nsScope != null && nsScope.AssociatedNamespace != null){
              UsedNamespaceList usedNamespaces = this.AssociatedNamespace.UsedNamespaces;
              int n = usedNamespaces == null ? 0 : usedNamespaces.Count;
              if (usedNamespaces != null)
                for (int i = 0; i < n; i++){
                  UsedNamespace usedNs = usedNamespaces[i];
                  if (usedNs == null) continue;
                  int key = usedNs.Namespace.UniqueIdKey;
                  if (alreadyUsed[key] != null) continue;
                  alreadyUsed[key] = usedNs.Namespace;
                  Identifier usedNestedNamespace = Identifier.For(usedNs.Namespace+"."+Namespace);
                  this.GetReferencedTypes(usedNestedNamespace, name, duplicates);
                }
            }
            scope = scope.OuterScope;
          }
          numDups = duplicates.Count;
          for (int i = numDups-1; i >= 0; i--){
            TypeNode dup = duplicates[i];
            if (dup == null || !dup.IsPublic) numDups--;
            result = dup;
          }
        }
        if (numDups <= 1) duplicates = null;
      }
      if (result == null)
        typeFor[name.UniqueIdKey] = Class.DoesNotExist;
      else
        typeFor[name.UniqueIdKey] = result;
      return result; 
    }
    /// <summary>
    /// Searches this namespace for a type with this name. Also considers aliases and used name spaces, including those of outer namespaces.
    /// If more than one type is found, a list is returned in duplicates. Types defined in the associated
    /// module mask types defined in referenced modules and assemblies. Results are cached and duplicates are returned only when
    /// there is a cache miss.
    /// </summary>
    public virtual TypeNode GetType(Identifier name, out TypeNodeList duplicates){
      return this.GetType(name, out duplicates, false);
    }
    public virtual TypeNode GetType(Identifier name, out TypeNodeList duplicates, bool returnNullIfHiddenByNestedNamespace){
      duplicates = null;
      if (name == null || this.typeFor == null || this.AssociatedNamespace == null || this.AssociatedModule == null){
        Debug.Assert(false); return null;
      }
      AssemblyNode associatedAssembly = this.AssociatedModule as AssemblyNode;
      TypeNode result = (TypeNode)this.typeFor[name.UniqueIdKey];
      if (result == Class.DoesNotExist) return null;
      if (result != null) return result;
      //If the associated module declares a type with the given name in this namespace, it wins
      result = this.AssociatedModule.GetType(this.AssociatedNamespace.FullNameId, name);
      if (result == null && returnNullIfHiddenByNestedNamespace){
        //Do not proceed to outer namespaces or look at aliases. The nested namespace hides these.
        Identifier fullName = name;
        if (this.AssociatedNamespace.FullName != null && this.AssociatedNamespace.Name.UniqueIdKey != Identifier.Empty.UniqueIdKey)
          fullName = Identifier.For(this.AssociatedNamespace.FullName+"."+name);
        if (this.AssociatedModule.IsValidNamespace(fullName))
          result = Class.DoesNotExist;
      }
      if (result == null){
        //If the namespace (or an outer namespace) has an alias definition with this name it wins. (Expected to be mutually exclusive with above.)        
        Scope scope = this;
        while (scope != null && result == null){
          NamespaceScope nsScope = scope as NamespaceScope;
          if (nsScope != null && nsScope.AliasedType != null)
            result = (TypeNode)nsScope.AliasedType[name.UniqueIdKey];
          if (result == null && returnNullIfHiddenByNestedNamespace && nsScope != null && 
          nsScope.AliasedNamespace != null && nsScope.AliasedNamespace[name.UniqueIdKey] != null)
            result = Class.DoesNotExist;
          scope = scope.OuterScope;
        }
      }
      if (result == null){
        //Now get into situations where there might be duplicates.
        duplicates = new TypeNodeList();
        //Check the used namespaces of this and outer namespace scopes
        TrivialHashtable alreadyUsed = new TrivialHashtable();
        Scope scope = this;
        while (scope != null) {
          NamespaceScope nsScope = scope as NamespaceScope;
          if (nsScope != null && nsScope.AssociatedNamespace != null && nsScope.AssociatedModule != null) {
            UsedNamespaceList usedNamespaces = nsScope.AssociatedNamespace.UsedNamespaces;
            int n = usedNamespaces == null ? 0 : usedNamespaces.Count;
            if (usedNamespaces != null)
              for (int i = 0; i < n; i++) {
                UsedNamespace usedNs = usedNamespaces[i];
                if (usedNs == null || usedNs.Namespace == null) continue;
                int key = usedNs.Namespace.UniqueIdKey;
                if (alreadyUsed[key] != null) continue;
                alreadyUsed[key] = usedNs.Namespace;
                result = this.AssociatedModule.GetType(usedNs.Namespace, name);
                //^ assert duplicates != null;
                if (result != null) duplicates.Add(result);
              }
          }
          if (returnNullIfHiddenByNestedNamespace) break; 
          scope = scope.OuterScope;
        }
        if (duplicates.Count > 0) result = duplicates[0];
      }
      if (result == null)
        //First see if the the current module has a class by this name in the empty namespace
        result = this.AssociatedModule.GetType(Identifier.Empty, name);
      if (result == null){
        //The associated module does not have a type by this name, so check its referenced modules and assemblies
        //First check this namespace
        this.GetReferencedTypes(this.AssociatedNamespace.FullNameId, name, duplicates);
        int numDups = duplicates.Count;
        if (numDups == 1){
          result = duplicates[0];
          if (this.IsNotAccessible(associatedAssembly, result)) { numDups--; result = null; }
        }else{
          for (int i = numDups-1; i >= 0; i--){
            TypeNode dup = duplicates[i];
            if (this.IsNotAccessible(associatedAssembly, dup)) { numDups--; continue; }
            result = dup;
          }
          if (numDups == 0 && duplicates.Count > 0){
            result = duplicates[0];
            numDups = duplicates.Count;
          }
        }
        if (numDups == 0){
          if (duplicates.Count > 0) duplicates = new TypeNodeList();
          //Check the used namespaces of this and outer namespace scopes
          TrivialHashtable alreadyUsed = new TrivialHashtable();
          Scope scope = this;
          while (scope != null) {
            NamespaceScope nsScope = scope as NamespaceScope;
            if (nsScope != null) {
              UsedNamespaceList usedNamespaces = nsScope.AssociatedNamespace.UsedNamespaces;
              int n = usedNamespaces == null ? 0 : usedNamespaces.Count;
              if (usedNamespaces != null)
                for (int i = 0; i < n; i++) {
                  UsedNamespace usedNs = usedNamespaces[i];
                  if (usedNs == null || usedNs.Namespace == null) continue;
                  int key = usedNs.Namespace.UniqueIdKey;
                  if (alreadyUsed[key] != null) continue;
                  alreadyUsed[key] = usedNs.Namespace;
                  this.GetReferencedTypes(usedNs.Namespace, name, duplicates);
                }
            }
            scope = scope.OuterScope;
            if (returnNullIfHiddenByNestedNamespace) break;
          }
          numDups = duplicates.Count;
          for (int i = numDups-1; i >= 0; i--){
            TypeNode dup = duplicates[i];
            if (this.IsNotAccessible(associatedAssembly, dup)) { 
              numDups--; continue;
            }
            result = dup;
          }
        }
        if (numDups == 0){
          if (duplicates.Count > 0) duplicates = new TypeNodeList();
          this.GetReferencedTypes(Identifier.Empty, name, duplicates);
          numDups = duplicates.Count;
          for (int i = numDups-1; i >= 0; i--){
            TypeNode dup = duplicates[i];
            if (this.IsNotAccessible(associatedAssembly, dup)) { 
              numDups--; continue;
            }
            result = dup;
          }
        }
        if (numDups <= 1) duplicates = null;
      }
      if (result == null)
        this.typeFor[name.UniqueIdKey] = Class.DoesNotExist;
      else
        this.typeFor[name.UniqueIdKey] = result;
      if (result == Class.DoesNotExist) return null;
      if (duplicates != null && duplicates.Count > 1 && this.AssociatedNamespace != null && this.AssociatedNamespace.Name != null && this.AssociatedNamespace.Name.Name != null) {
        result = null;
        for (int i = 0, n = duplicates.Count; i < n; i++) {
          TypeNode t = duplicates[i];
          if (t == null || t.Namespace == null) continue;
          if (this.AssociatedNamespace.Name.Name.StartsWith(t.Namespace.Name)) {
            if (result != null) {
              result = null;
              break;
            }
            result = t;
          }
        }
        if (result != null)
          duplicates = null;
        else
          result = duplicates[0];
      }
      return result; 
    }
    private bool IsNotAccessible(AssemblyNode associatedAssembly, TypeNode dup) {
      if (dup == null) return false;
      return !dup.IsPublic && (associatedAssembly == null || 
              !associatedAssembly.MayAccessInternalTypesOf(dup.DeclaringModule as AssemblyNode)) && !this.AssociatedModule.ContainsModule(dup.DeclaringModule);
    }
    /// <summary>
    /// Searches the module and assembly references of the associated module to find types
    /// </summary>
    public virtual void GetReferencedTypes(Identifier Namespace, Identifier name, TypeNodeList types){
      if (Namespace == null || name == null || types == null || this.AssociatedModule == null) {Debug.Assert(false); return;}
      AssemblyReferenceList arefs = this.AssociatedModule.AssemblyReferences;
      for (int i = 0, n = arefs == null ? 0 : arefs.Count; i < n; i++){
        AssemblyReference ar = arefs[i];
        if (ar == null || ar.Assembly == null) continue;
        TypeNode t = ar.Assembly.GetType(Namespace, name);
        if (t == null) continue;
        //TODO: deal with type forwarding
        types.Add(t);
      }
      ModuleReferenceList mrefs = this.AssociatedModule.ModuleReferences;
      if (mrefs != null)
        for (int i = 0, n = mrefs.Count; i < n; i++){
          ModuleReference mr = mrefs[i];
          if (mr == null || mr.Module == null) continue;
          TypeNode t = mr.Module.GetType(Namespace, name);
          if (t == null) continue;
          types.Add(t);
        }
    }
  }
#endif
    public class DelegateNode : TypeNode
    {
        internal static readonly DelegateNode /*!*/
            Dummy = new DelegateNode();

        protected ParameterList parameters;
        protected TypeNode returnType;
#if !MinimalReader
        public TypeNode ReturnTypeExpression;
#endif
        public DelegateNode()
            : base(NodeType.DelegateNode)
        {
        }

        public DelegateNode(NestedTypeProvider provideNestedTypes, TypeAttributeProvider provideAttributes,
            TypeMemberProvider provideMembers, object handle)
            : base(NodeType.DelegateNode, provideNestedTypes, provideAttributes, provideMembers, handle)
        {
        }

        public virtual ParameterList Parameters
        {
            get
            {
                var pList = parameters;
                if (pList == null)
                {
                    var members = Members; //Evaluate for side effect
                    if (members != null) members = null;
                    lock (this)
                    {
                        if (parameters != null) return parameters;
                        var invokers = GetMembersNamed(StandardIds.Invoke);
                        for (int i = 0, n = invokers.Count; i < n; i++)
                        {
                            var m = invokers[i] as Method;
                            if (m == null) continue;
                            parameters = pList = m.Parameters;
                            returnType = m.ReturnType;
                            break;
                        }
                    }
                }

                return pList;
            }
            set { parameters = value; }
        }

        public virtual TypeNode ReturnType
        {
            get
            {
                var rt = returnType;
                if (rt == null)
                {
                    var pars = Parameters; //Evaluate for side effect
                    if (pars != null) pars = null;
                    rt = returnType;
                }

                return rt;
            }
            set { returnType = value; }
        }
#if !MinimalReader
        public DelegateNode(Module declaringModule, TypeNode declaringType, AttributeList attributes, TypeFlags flags,
            Identifier Namespace, Identifier name, TypeNode returnType, ParameterList parameters)
            : base(declaringModule, declaringType, attributes, flags, Namespace, name, null, null,
                NodeType.DelegateNode)
        {
            this.parameters = parameters;
            this.returnType = returnType;
        }

        private bool membersAlreadyProvided;
        public virtual void ProvideMembers()
        {
            if (membersAlreadyProvided) return;
            membersAlreadyProvided = true;
            memberCount = 0;
            var members = this.members = new MemberList();
            //ctor
            var parameters = new ParameterList(2);
            parameters.Add(new Parameter(null, ParameterFlags.None, StandardIds.Object, CoreSystemTypes.Object, null,
                null));
            parameters.Add(new Parameter(null, ParameterFlags.None, StandardIds.Method, CoreSystemTypes.IntPtr, null,
                null));
            var ctor = new InstanceInitializer(this, null, parameters, null);
            ctor.Flags |= MethodFlags.Public | MethodFlags.HideBySig;
            ctor.CallingConvention = CallingConventionFlags.HasThis;
            ctor.ImplFlags = MethodImplFlags.Runtime;
            members.Add(ctor);
            //Invoke
            var invoke = new Method(this, null, StandardIds.Invoke, Parameters, ReturnType, null);
            invoke.Flags = MethodFlags.Public | MethodFlags.HideBySig | MethodFlags.Virtual | MethodFlags.NewSlot;
            invoke.CallingConvention = CallingConventionFlags.HasThis;
            invoke.ImplFlags = MethodImplFlags.Runtime;
            members.Add(invoke);
            // Skip adding async methods if AsyncCallback is a dummy.
            if (SystemTypes.AsyncCallback.ReturnType != null)
            {
                //BeginInvoke
                var dparams = this.parameters;
                var n = dparams == null ? 0 : dparams.Count;
                parameters = new ParameterList(n + 2);
                for (var i = 0; i < n; i++)
                {
                    //^ assert dparams != null;
                    var p = dparams[i];
                    if (p == null) continue;
                    parameters.Add((Parameter)p.Clone());
                }

                parameters.Add(new Parameter(null, ParameterFlags.None, StandardIds.callback, SystemTypes.AsyncCallback,
                    null, null));
                parameters.Add(new Parameter(null, ParameterFlags.None, StandardIds.Object, CoreSystemTypes.Object,
                    null, null));
                var beginInvoke = new Method(this, null, StandardIds.BeginInvoke, parameters, SystemTypes.IASyncResult,
                    null);
                beginInvoke.Flags = MethodFlags.Public | MethodFlags.HideBySig | MethodFlags.NewSlot |
                                    MethodFlags.Virtual;
                beginInvoke.CallingConvention = CallingConventionFlags.HasThis;
                beginInvoke.ImplFlags = MethodImplFlags.Runtime;
                members.Add(beginInvoke);
                //EndInvoke
                parameters = new ParameterList(1);
                for (var i = 0; i < n; i++)
                {
                    var p = dparams[i];
                    if (p == null || p.Type == null || !(p.Type is Reference)) continue;
                    parameters.Add((Parameter)p.Clone());
                }

                parameters.Add(new Parameter(null, ParameterFlags.None, StandardIds.result, SystemTypes.IASyncResult,
                    null, null));
                var endInvoke = new Method(this, null, StandardIds.EndInvoke, parameters, ReturnType, null);
                endInvoke.Flags = MethodFlags.Public | MethodFlags.HideBySig | MethodFlags.NewSlot |
                                  MethodFlags.Virtual;
                endInvoke.CallingConvention = CallingConventionFlags.HasThis;
                endInvoke.ImplFlags = MethodImplFlags.Runtime;
                members.Add(endInvoke);
            }

            if (!IsGeneric)
            {
                var templPars = TemplateParameters;
                for (int i = 0, m = templPars == null ? 0 : templPars.Count; i < m; i++)
                {
                    //^ assert templPars != null;
                    var tpar = templPars[i];
                    if (tpar == null) continue;
                    members.Add(tpar);
                }
            }
        }
#endif
    }
#if !MinimalReader
    public class FunctionType : DelegateNode
    {
        protected TypeNodeList structuralElementTypes;

        private FunctionType(Identifier name, TypeNode returnType, ParameterList parameters)
        {
            Flags = TypeFlags.Public | TypeFlags.Sealed;
            Namespace = StandardIds.StructuralTypes;
            Name = name;
            this.returnType = returnType;
            this.parameters = parameters;
        }

        public override bool IsStructural => true;

        public override TypeNodeList StructuralElementTypes
        {
            get
            {
                var result = structuralElementTypes;
                if (result != null) return result;
                structuralElementTypes = result = new TypeNodeList();
                result.Add(ReturnType);
                var pars = Parameters;
                for (int i = 0, n = pars == null ? 0 : pars.Count; i < n; i++)
                {
                    var par = pars[i];
                    if (par == null || par.Type == null) continue;
                    result.Add(par.Type);
                }

                return result;
            }
        }

        public static FunctionType For(TypeNode returnType, ParameterList parameters, TypeNode referringType)
        {
            if (returnType == null || referringType == null) return null;
            var module = referringType.DeclaringModule;
            if (module == null) return null;
            var visibility = returnType.Flags & TypeFlags.VisibilityMask;
            var name = new StringBuilder();
            name.Append("Function_");
            name.Append(returnType.Name);
            var n = parameters == null ? 0 : parameters.Count;
            if (parameters != null)
                for (var i = 0; i < n; i++)
                {
                    var p = parameters[i];
                    if (p == null || p.Type == null) continue;
                    visibility = GetVisibilityIntersection(visibility, p.Type.Flags & TypeFlags.VisibilityMask);
                    name.Append('_');
                    name.Append(p.Type.Name);
                }

            FunctionType func = null;
            var count = 0;
            var fNameString = name.ToString();
            var fName = Identifier.For(fNameString);
            var result = module.GetStructurallyEquivalentType(StandardIds.StructuralTypes, fName);
            while (result != null)
            {
                //Mangled name is the same. But mangling is not unique (types are not qualified with assemblies), so check for equality.
                func = result as FunctionType;
                var goodMatch = func != null && func.ReturnType == returnType;
                if (goodMatch)
                {
                    //^ assert func != null;
                    var fpars = func.Parameters;
                    var m = fpars == null ? 0 : fpars.Count;
                    goodMatch = n == m;
                    if (parameters != null && fpars != null)
                        for (var i = 0; i < n && goodMatch; i++)
                        {
                            var p = parameters[i];
                            var q = fpars[i];
                            goodMatch = p != null && q != null && p.Type == q.Type;
                        }
                }

                if (goodMatch) return func;
                //Mangle some more
                fName = Identifier.For(fNameString + (++count));
                result = module.GetStructurallyEquivalentType(StandardIds.StructuralTypes, fName);
            }

            if (parameters != null)
            {
                var clonedParams = new ParameterList(n);
                for (var i = 0; i < n; i++)
                {
                    var p = parameters[i];
                    if (p != null) p = (Parameter)p.Clone();
                    clonedParams.Add(p);
                }

                parameters = clonedParams;
            }

            func = new FunctionType(fName, returnType, parameters);
            func.DeclaringModule = module;
            switch (visibility)
            {
                case TypeFlags.NestedFamANDAssem:
                case TypeFlags.NestedFamily:
                case TypeFlags.NestedPrivate:
                    referringType.Members.Add(func);
                    func.DeclaringType = referringType;
                    func.Flags &= ~TypeFlags.VisibilityMask;
                    func.Flags |= TypeFlags.NestedPrivate;
                    break;
                default:
                    module.Types.Add(func);
                    break;
            }

            module.StructurallyEquivalentType[func.Name.UniqueIdKey] = func;
            func.ProvideMembers();
            return func;
        }

        public override bool IsStructurallyEquivalentTo(TypeNode type, Func<TypeNode, TypeNode> typeSubstitution = null)
        {
            if (type == null) return false;
            if (this == type) return true;
            var t = type as FunctionType;
            if (t == null) return false;
            if (Template != null) return base.IsStructurallyEquivalentTo(t, typeSubstitution);
            if (Flags != t.Flags) return false;
            if (ReturnType == null || t.ReturnType == null) return false;
            if (ReturnType != t.ReturnType &&
                !ReturnType.IsStructurallyEquivalentTo(t.ReturnType, typeSubstitution)) return false;
            if (Parameters == null) return t.Parameters == null;
            if (t.Parameters == null) return false;
            var n = Parameters.Count;
            if (n != t.Parameters.Count) return false;
            for (var i = 0; i < n; i++)
            {
                var p1 = Parameters[i];
                var p2 = t.Parameters[i];
                if (p1 == null || p2 == null) return false;
                if (p1.Type == null || p2.Type == null) return false;
                if (p1.Type != p2.Type && !p1.Type.IsStructurallyEquivalentTo(p2.Type, typeSubstitution)) return false;
            }

            return true;
        }
    }
#endif
    public class EnumNode : TypeNode
    {
        internal static readonly EnumNode /*!*/
            Dummy = new EnumNode();

        public EnumNode()
            : base(NodeType.EnumNode)
        {
            typeCode = ElementType.ValueType;
            Flags |= TypeFlags.Sealed;
        }

        public EnumNode(NestedTypeProvider provideNestedTypes, TypeAttributeProvider provideAttributes,
            TypeMemberProvider provideMembers, object handle)
            : base(NodeType.EnumNode, provideNestedTypes, provideAttributes, provideMembers, handle)
        {
            typeCode = ElementType.ValueType;
            Flags |= TypeFlags.Sealed;
        }
#if !MinimalReader
        public EnumNode(Module declaringModule, TypeNode declaringType, AttributeList attributes,
            TypeFlags typeAttributes,
            Identifier Namespace, Identifier name, InterfaceList interfaces, MemberList members)
            : base(declaringModule, declaringType, attributes, typeAttributes, Namespace, name, interfaces, members,
                NodeType.EnumNode)
        {
            typeCode = ElementType.ValueType;
            Flags |= TypeFlags.Sealed;
        }
#endif
        public override bool IsUnmanaged => true;
        protected internal TypeNode underlyingType;
        /// <summary>
        ///     The underlying integer type used to store values of this enumeration.
        /// </summary>
        public virtual TypeNode UnderlyingType
        {
            get
            {
                if (underlyingType == null)
                {
                    if (template is EnumNode)
                        return underlyingType = ((EnumNode)template).UnderlyingType;
                    underlyingType = CoreSystemTypes.Int32;
                    var members = Members;
                    for (int i = 0, n = members.Count; i < n; i++)
                    {
                        var mem = members[i];
                        var f = mem as Field;
                        if (f != null && (f.Flags & FieldFlags.Static) == 0)
                            return underlyingType = f.Type;
                    }
                }

                return underlyingType;
            }
            set
            {
                underlyingType = value;
                var members = Members;
                for (int i = 0, n = members.Count; i < n; i++)
                {
                    var mem = members[i];
                    var f = mem as Field;
                    if (f != null && (f.Flags & FieldFlags.Static) == 0)
                    {
                        f.Type = value;
                        return;
                    }
                }

                Members.Add(new Field(this, null, FieldFlags.Public | FieldFlags.SpecialName | FieldFlags.RTSpecialName,
                    StandardIds.Value__, value, null));
            }
        }
#if ExtendedRuntime
    public override bool IsPointerFree
    {
      get
      {
        return true;
      }
    }
#endif
#if !MinimalReader
        public TypeNode UnderlyingTypeExpression;
#endif
    }
#if FxCop
  public class InterfaceNode : TypeNode{
#else
    public class Interface : TypeNode
    {
#endif
        protected TrivialHashtable jointMemberTable;
        protected MemberList jointDefaultMembers;

        internal static readonly Interface /*!*/
            Dummy = new Interface();

#if FxCop
    public InterfaceNode()
      : base(NodeType.Interface){
      this.Flags = TypeFlags.Interface|TypeFlags.Abstract;
    }
    public InterfaceNode(InterfaceList baseInterfaces)
      : base(NodeType.Interface){
      this.Interfaces = baseInterfaces;
      this.Flags = TypeFlags.Interface|TypeFlags.Abstract;
    }
    public InterfaceNode(InterfaceList baseInterfaces, NestedTypeProvider provideNestedTypes, TypeAttributeProvider provideAttributes, TypeMemberProvider provideMembers, object handle)
      : base(NodeType.Interface, provideNestedTypes, provideAttributes, provideMembers, handle){
      this.Interfaces = baseInterfaces;
    }
#else
        public Interface()
            : base(NodeType.Interface)
        {
            Flags = TypeFlags.Interface | TypeFlags.Abstract;
        }

        public Interface(InterfaceList baseInterfaces)
            : base(NodeType.Interface)
        {
            Interfaces = baseInterfaces;
            Flags = TypeFlags.Interface | TypeFlags.Abstract;
        }

        public Interface(InterfaceList baseInterfaces, NestedTypeProvider provideNestedTypes,
            TypeAttributeProvider provideAttributes, TypeMemberProvider provideMembers, object handle)
            : base(NodeType.Interface, provideNestedTypes, provideAttributes, provideMembers, handle)
        {
            Interfaces = baseInterfaces;
        }
#endif
#if !MinimalReader
        public Interface(Module declaringModule, TypeNode declaringType, AttributeList attributes, TypeFlags flags,
            Identifier Namespace, Identifier name, InterfaceList baseInterfaces, MemberList members)
            : base(declaringModule, declaringType, attributes, flags, Namespace, name, baseInterfaces, members,
                NodeType.Interface)
        {
            Flags |= TypeFlags.Interface | TypeFlags.Abstract;
        }

        public override void GetAbstractMethods(MethodList /*!*/ result)
        {
            var members = Members;
            if (members == null) return;
            for (int i = 0, n = members.Count; i < n; i++)
            {
                var m = members[i] as Method;
                if (m != null) result.Add(m);
            }
        }

        public virtual MemberList GetAllDefaultMembers()
        {
            if (jointDefaultMembers == null)
            {
                jointDefaultMembers = new MemberList();
                var defs = DefaultMembers;
                for (int i = 0, n = defs == null ? 0 : defs.Count; i < n; i++)
                    jointDefaultMembers.Add(defs[i]);
                var interfaces = Interfaces;
                if (interfaces != null)
                    for (int j = 0, m = interfaces.Count; j < m; j++)
                    {
                        var iface = interfaces[j];
                        if (iface == null) continue;
                        defs = iface.GetAllDefaultMembers();
                        if (defs == null) continue;
                        for (int i = 0, n = defs.Count; i < n; i++)
                            jointDefaultMembers.Add(defs[i]);
                    }
            }

            return jointDefaultMembers;
        }

        public virtual MemberList GetAllMembersNamed(Identifier /*!*/ name)
        {
            lock (this)
            {
                var memberTable = jointMemberTable;
                if (memberTable == null) jointMemberTable = memberTable = new TrivialHashtable();
                var result = (MemberList)memberTable[name.UniqueIdKey];
                if (result != null) return result;
                memberTable[name.UniqueIdKey] = result = new MemberList();
                var members = GetMembersNamed(name);
                for (int i = 0, n = members == null ? 0 : members.Count; i < n; i++)
                    result.Add(members[i]);
                var interfaces = Interfaces;
                for (int j = 0, m = interfaces == null ? 0 : interfaces.Count; j < m; j++)
                {
                    var iface = interfaces[j];
                    if (iface == null) continue;
                    members = iface.GetAllMembersNamed(name);
                    if (members != null)
                        for (int i = 0, n = members.Count; i < n; i++)
                            result.Add(members[i]);
                }

                members = CoreSystemTypes.Object.GetMembersNamed(name);
                for (int i = 0, n = members == null ? 0 : members.Count; i < n; i++)
                    result.Add(members[i]);
                return result;
            }
        }
#endif
    }

    public class Struct : TypeNode
    {
        internal static readonly Struct /*!*/
            Dummy = new Struct();

        public Struct()
            : base(NodeType.Struct)
        {
            typeCode = ElementType.ValueType;
            Flags = TypeFlags.Sealed;
        }

        public Struct(NestedTypeProvider provideNestedTypes, TypeAttributeProvider provideAttributes,
            TypeMemberProvider provideMembers, object handle)
            : base(NodeType.Struct, provideNestedTypes, provideAttributes, provideMembers, handle)
        {
            typeCode = ElementType.ValueType;
        }
#if !MinimalReader
        public Struct(Module declaringModule, TypeNode declaringType, AttributeList attributes, TypeFlags flags,
            Identifier Namespace, Identifier name, InterfaceList interfaces, MemberList members)
            : base(declaringModule, declaringType, attributes, flags, Namespace, name, interfaces, members,
                NodeType.Struct)
        {
            Interfaces = interfaces;
            typeCode = ElementType.ValueType;
            Flags |= TypeFlags.Sealed;
        }

        protected bool cachedUnmanaged;
        protected bool cachedUnmanagedIsValid;
        /// <summary>True if the type is a value type containing only fields of unmanaged types.</summary>
        public override bool IsUnmanaged
        {
            get
            {
                if (cachedUnmanagedIsValid) return cachedUnmanaged;
                cachedUnmanagedIsValid = true; //protect against cycles
                cachedUnmanaged = true; //Self references should not influence the answer
                if (IsPrimitive) return cachedUnmanaged = true;
                var members = Members;
                var isUnmanaged = true;
                for (int i = 0, n = members == null ? 0 : members.Count; i < n; i++)
                {
                    var f = members[i] as Field;
                    if (f == null || f.Type == null || f.IsStatic) continue;
                    if (!f.Type.IsUnmanaged)
                    {
                        isUnmanaged = false;
                        break;
                    }
                }
#if ExtendedRuntime
        this.cachedUnmanaged = isUnmanaged || IsPointerFree;
#else
                cachedUnmanaged = isUnmanaged;
#endif
                return cachedUnmanaged;
            }
        }
#endif
#if ExtendedRuntime
    protected bool cachedPointerFree;
    protected bool cachedPointerFreeIsValid;
    /// <summary>True if the type is a value type containing no managed or unmanaged pointers.</summary>
    public override bool IsPointerFree
    {
      get
      {
        if (this.cachedPointerFreeIsValid) return this.cachedPointerFree;
        // Note: not threadsafe
        this.cachedPointerFreeIsValid = true; //protect against cycles
        this.cachedPointerFree = true; //Self references should not influence the answer
        if (this.IsPrimitive) return this.cachedPointerFree = true;
        MemberList members = this.Members;
        bool isPointerFree = true;
        for (int i = 0, n = members == null ? 0 : members.Count; i < n; i++)
        {
          Field f = members[i] as Field;
          if (f == null || f.Type == null || f.IsStatic) continue;
          if (!f.Type.IsPointerFree) { isPointerFree = false; break; }
        }
        return this.cachedPointerFree = isPointerFree;
      }
    }
#endif
    }

    public interface ITypeParameter
    {
        Member DeclaringMember { get; set; }

        /// <summary>
        ///     Zero based index into a parameter list containing this parameter.
        /// </summary>
        int ParameterListIndex { get; set; }

        TypeParameterFlags TypeParameterFlags { get; set; }
        bool IsCovariant { get; }
        bool IsContravariant { get; }
        bool IsUnmanaged { get; }
#if ExtendedRuntime
    bool IsPointerFree { get; }
#endif
#if !MinimalReader
        Identifier Name { get; }
        Module DeclaringModule { get; }
        TypeNode DeclaringType { get; }
        SourceContext SourceContext { get; }
        int UniqueKey { get; }
        TypeFlags Flags { get; }
#endif
    }

    public class TypeParameter : Interface, ITypeParameter
    {
        public TypeParameter()
        {
            NodeType = NodeType.TypeParameter;
            Flags = TypeFlags.Interface | TypeFlags.NestedPublic | TypeFlags.Abstract;
            Namespace = StandardIds.TypeParameter;
        }

        public TypeParameter(InterfaceList baseInterfaces, NestedTypeProvider provideNestedTypes,
            TypeAttributeProvider provideAttributes, TypeMemberProvider provideMembers, object handle)
            : base(baseInterfaces, provideNestedTypes, provideAttributes, provideMembers, handle)
        {
            NodeType = NodeType.TypeParameter;
            Flags = TypeFlags.Interface | TypeFlags.NestedPublic | TypeFlags.Abstract;
            Namespace = StandardIds.TypeParameter;
        }

        public Member DeclaringMember { get; set; }

#if !NoReflection && WHIDBEY
        public override Type GetRuntimeType()
        {
            var t = DeclaringMember as TypeNode;
            if (t == null) return null;
            var rt = t.GetRuntimeType();
            if (rt == null) return null;
            var typeParameters = rt.GetGenericArguments();
            if (ParameterListIndex >= typeParameters.Length) return null;
            return typeParameters[ParameterListIndex];
        }
#endif
        /// <summary>
        ///     Zero based index into a parameter list containing this parameter.
        /// </summary>
        public int ParameterListIndex { get; set; }

#if ExtendedRuntime
    private bool typeParameterFlagsIsValid = false;
#endif
        public TypeParameterFlags TypeParameterFlags
        {
            get
            {
#if ExtendedRuntime
        if (!typeParameterFlagsIsValid) {
          // check if we have the corresponding attribute
          for (int i = 0; i < (this.Attributes == null?0:this.Attributes.Count); i++) {
            if (this.Attributes[i].Type == SystemTypes.TemplateParameterFlagsAttribute) {
              Literal lit = this.Attributes[i].Expressions[0] as Literal;
              if (lit != null && lit.Value is int) {
                this.typeParameterFlags = (TypeParameterFlags)((int)lit.Value);
              }
              break;
            }
          }
          this.typeParameterFlagsIsValid = true;
        }
#endif
                return typeParameterFlags;
            }
            set
            {
                typeParameterFlags = value;
#if ExtendedRuntime
        this.typeParameterFlagsIsValid = true;
#endif
            }
        }

        private TypeParameterFlags typeParameterFlags;

        public bool IsCovariant =>
            (typeParameterFlags & TypeParameterFlags.VarianceMask) == TypeParameterFlags.Covariant;

        public bool IsContravariant =>
            (typeParameterFlags & TypeParameterFlags.VarianceMask) == TypeParameterFlags.Contravariant;

        public override bool IsStructural => true;

        /// <summary>True if the type serves as a parameter to a type template.</summary>
        public override bool IsTemplateParameter => true;

        public override bool IsValueType => (TypeParameterFlags & TypeParameterFlags.ValueTypeConstraint) ==
                                            TypeParameterFlags.ValueTypeConstraint;

        public override bool IsReferenceType => (TypeParameterFlags & TypeParameterFlags.ReferenceTypeConstraint) ==
                                                TypeParameterFlags.ReferenceTypeConstraint;
#if ExtendedRuntime
    private bool isUnmanagedIsValid = false;
    private bool isUnmanaged = false;
    public override bool IsUnmanaged{
      get{
        if (!isUnmanagedIsValid && SystemTypes.UnmanagedStructTemplateParameterAttribute != null){
          // check if we have the corresponding attribute
          for (int i = 0; i < (this.Attributes == null?0:this.Attributes.Count); i++){
            AttributeNode attr = this.Attributes[i];
            if (attr == null) continue;
            if (attr.Type == SystemTypes.UnmanagedStructTemplateParameterAttribute){
              isUnmanaged = true;
              break;
            }
#if ExtendedRuntime
            if (!isUnmanaged) { isUnmanaged = IsPointerFree; }
#endif
          }
          isUnmanagedIsValid = true;
        }
        return isUnmanaged;
      }
    }
    public void SetIsUnmanaged(){
      this.isUnmanaged = true;
      this.isUnmanagedIsValid = true;
    }
    private bool isPointerFreeIsValid = false;
    private bool isPointerFree = false;
    public override bool IsPointerFree
    {
      get
      {
        if (!isPointerFreeIsValid && SystemTypes.PointerFreeStructTemplateParameterAttribute != null)
        {
          // check if we have the corresponding attribute
          for (int i = 0; i < (this.Attributes == null ? 0 : this.Attributes.Count); i++)
          {
            AttributeNode attr = this.Attributes[i];
            if (attr == null) continue;
            if (attr.Type == SystemTypes.PointerFreeStructTemplateParameterAttribute)
            {
              isPointerFree = true;
              break;
            }
          }
          isPointerFreeIsValid = true;
        }
        return isPointerFree;
      }
    }
    public void SetIsPointerFree()
    {
      this.isPointerFree = true;
      this.isPointerFreeIsValid = true;
      // implies unmanaged
      SetIsUnmanaged();
    }
#endif
#if !NoXml
        public override XmlNode Documentation
        {
            get
            {
                if (documentation == null && DeclaringMember != null && Name != null)
                {
                    var parentDoc = DeclaringMember.Documentation;
                    if (parentDoc != null && parentDoc.HasChildNodes)
                    {
                        var myName = Name.Name;
                        foreach (XmlNode child in parentDoc.ChildNodes)
                            if (child.Name == "typeparam" && child.Attributes != null)
                                foreach (XmlAttribute attr in child.Attributes)
                                    if (attr != null && attr.Name == "name" && attr.Value == myName)
                                        return documentation = child;
                    }
                }

                return documentation;
            }
            set { documentation = value; }
        }

        public override string HelpText
        {
            get
            {
                if (helpText == null)
                {
                    var doc = Documentation;
                    if (doc != null) helpText = doc.InnerText;
                }

                return helpText;
            }
            set { helpText = value; }
        }
#endif
        protected internal TypeNodeList structuralElementTypes;
        public override TypeNodeList StructuralElementTypes
        {
            get
            {
                var result = structuralElementTypes;
                if (result != null) return result;
                structuralElementTypes = result = new TypeNodeList();
                if (BaseType != null) result.Add(BaseType);
                var interfaces = Interfaces;
                for (int i = 0, n = interfaces == null ? 0 : interfaces.Count; i < n; i++)
                {
                    var iface = interfaces[i];
                    if (iface == null) continue;
                    result.Add(iface);
                }

                return result;
            }
        }
#if !NoXml
        internal override void AppendDocumentIdMangledName(StringBuilder /*!*/ sb, TypeNodeList methodTypeParameters,
            TypeNodeList typeParameters)
        {
            if (TargetPlatform.GenericTypeNamesMangleChar != 0)
            {
                var n = methodTypeParameters == null ? 0 : methodTypeParameters.Count;
                for (var i = 0; i < n; i++)
                {
                    //^ assert methodTypeParameters != null;
                    var mpar = methodTypeParameters[i];
                    if (mpar != this) continue;
                    sb.Append(TargetPlatform.GenericTypeNamesMangleChar);
                    sb.Append(TargetPlatform.GenericTypeNamesMangleChar);
                    sb.Append(i);
                    return;
                }

                n = typeParameters == null ? 0 : typeParameters.Count;
                for (var i = 0; i < n; i++)
                {
                    var tpar = typeParameters[i];
                    if (tpar != this) continue;
                    sb.Append(TargetPlatform.GenericTypeNamesMangleChar);
                    sb.Append(i);
                    return;
                }

                sb.Append("not found:");
            }

            sb.Append(FullName);
        }
#endif
        public override string GetFullUnmangledNameWithoutTypeParameters()
        {
            return GetUnmangledNameWithoutTypeParameters();
        }

        public override string GetFullUnmangledNameWithTypeParameters()
        {
            return GetUnmangledNameWithTypeParameters();
        }

        public override bool IsStructurallyEquivalentTo(TypeNode type, Func<TypeNode, TypeNode> typeSubstitution = null)
        {
            if (null == (object)type) return false;
            if (this == type) return true;
            if (typeSubstitution != null && this == typeSubstitution(type)) return true;
            var itype = type as ITypeParameter;
            if (null == itype) return false;
            //if (type is MethodTypeParameter || type is MethodClassParameter) return false;
            if (Name != null && type.Name != null && Name.UniqueIdKey != type.Name.UniqueIdKey)
                if (DeclaringMember == itype.DeclaringMember)
                    return false;
            var bType = BaseType;
            var tbType = type.BaseType;
            if (null == (object)bType) bType = CoreSystemTypes.Object;
            if (null == (object)tbType) tbType = CoreSystemTypes.Object;
            if (bType != tbType /*&& !bType.IsStructurallyEquivalentTo(tbType)*/) return false;
            if (Interfaces == null) return type.Interfaces == null || type.Interfaces.Count == 0;
            if (type.Interfaces == null) return Interfaces.Count == 0;
            var n = Interfaces.Count;
            if (n != type.Interfaces.Count) return false;
            for (var i = 0; i < n; i++)
            {
                var i1 = Interfaces[i];
                var i2 = type.Interfaces[i];
                if (null == (object)i1 || null == (object)i2) return false;
                if (i1 != i2 /*&& !i1.IsStructurallyEquivalentTo(i2)*/) return false;
            }

            return true;
        }
#if !MinimalReader
        Module ITypeParameter.DeclaringModule => DeclaringModule;
        TypeFlags ITypeParameter.Flags => Flags;
        SourceContext ITypeParameter.SourceContext => SourceContext;
#endif
#if FxCop
    internal override void GetName(TypeFormat options, StringBuilder name)
    {
      if (options.TypeName == TypeNameFormat.FullyQualified)
      {
        TypeFormat typeFormat = options.Clone();
        typeFormat.TypeName = TypeNameFormat.Short;
        base.GetName(typeFormat, name);
        return;
      }
      base.GetName(options, name);
    }
#endif
    }

    public class MethodTypeParameter : TypeParameter
    {
        public MethodTypeParameter()
        {
            NodeType = NodeType.TypeParameter;
            Flags = TypeFlags.Interface | TypeFlags.NestedPublic | TypeFlags.Abstract;
            Namespace = StandardIds.TypeParameter;
        }
#if !MinimalReader
        public MethodTypeParameter(InterfaceList baseInterfaces, NestedTypeProvider provideNestedTypes,
            TypeAttributeProvider provideAttributes, TypeMemberProvider provideMembers, object handle)
            : base(baseInterfaces, provideNestedTypes, provideAttributes, provideMembers, handle)
        {
            NodeType = NodeType.TypeParameter;
            Flags = TypeFlags.Interface | TypeFlags.NestedPublic | TypeFlags.Abstract;
            Namespace = StandardIds.TypeParameter;
        }
#endif
#if !NoReflection
#if WHIDBEY
        public override Type GetRuntimeType()
        {
            var m = DeclaringMember as Method;
            if (m == null) return null;
            var mi = m.GetMethodInfo();
            if (mi == null) return null;
            var typeParameters = mi.GetGenericArguments();
            if (ParameterListIndex >= typeParameters.Length) return null;
            return typeParameters[ParameterListIndex];
        }
#endif
#endif
        public override bool IsStructurallyEquivalentTo(TypeNode type, Func<TypeNode, TypeNode> typeSubstitution = null)
        {
            if (ReferenceEquals(this, type)) return true;
            if (typeSubstitution != null && this == typeSubstitution(type)) return true;
            var tp = type as ITypeParameter;
            if (tp == null) return false;
            if (type is MethodTypeParameter || type is MethodClassParameter)
                return ParameterListIndex == tp.ParameterListIndex;
            return base.IsStructurallyEquivalentTo(type as MethodTypeParameter, typeSubstitution);
        }
    }

    public class ClassParameter : Class, ITypeParameter
    {
        protected TrivialHashtable jointMemberTable;

        public ClassParameter()
        {
            NodeType = NodeType.ClassParameter;
            baseClass = CoreSystemTypes.Object;
            Flags = TypeFlags.NestedPublic | TypeFlags.Abstract;
            Namespace = StandardIds.TypeParameter;
        }

        public ClassParameter(NestedTypeProvider provideNestedTypes, TypeAttributeProvider provideAttributes,
            TypeMemberProvider provideMembers, object handle)
            : base(provideNestedTypes, provideAttributes, provideMembers, handle)
        {
            NodeType = NodeType.ClassParameter;
            baseClass = CoreSystemTypes.Object;
            Flags = TypeFlags.NestedPrivate | TypeFlags.Abstract | TypeFlags.SpecialName;
            Namespace = StandardIds.TypeParameter;
        }

        public Member DeclaringMember { get; set; }

#if !MinimalReader
        public virtual MemberList GetAllMembersNamed(Identifier /*!*/ name)
        {
            lock (this)
            {
                var memberTable = jointMemberTable;
                if (memberTable == null) jointMemberTable = memberTable = new TrivialHashtable();
                var result = (MemberList)memberTable[name.UniqueIdKey];
                if (result != null) return result;
                memberTable[name.UniqueIdKey] = result = new MemberList();
                TypeNode t = this;
                while (t != null)
                {
                    var members = t.GetMembersNamed(name);
                    if (members != null)
                        for (int i = 0, n = members.Count; i < n; i++)
                            result.Add(members[i]);
                    t = t.BaseType;
                }

                var interfaces = Interfaces;
                if (interfaces != null)
                    for (int j = 0, m = interfaces.Count; j < m; j++)
                    {
                        var iface = interfaces[j];
                        if (iface == null) continue;
                        members = iface.GetAllMembersNamed(name);
                        if (members != null)
                            for (int i = 0, n = members == null ? 0 : members.Count; i < n; i++)
                                result.Add(members[i]);
                    }

                members = CoreSystemTypes.Object.GetMembersNamed(name);
                if (members != null)
                    for (int i = 0, n = members.Count; i < n; i++)
                        result.Add(members[i]);
                return result;
            }
        }
#endif

#if !NoReflection && WHIDBEY
        public override Type GetRuntimeType()
        {
            var t = DeclaringMember as TypeNode;
            if (t == null) return null;
            var rt = t.GetRuntimeType();
            if (rt == null) return null;
            var typeParameters = rt.GetGenericArguments();
            if (ParameterListIndex >= typeParameters.Length) return null;
            return typeParameters[ParameterListIndex];
        }
#endif

        /// <summary>
        ///     Zero based index into a parameter list containing this parameter.
        /// </summary>
        public int ParameterListIndex { get; set; }

        public TypeParameterFlags TypeParameterFlags { get; set; }

        public bool IsCovariant =>
            (TypeParameterFlags & TypeParameterFlags.VarianceMask) == TypeParameterFlags.Covariant;

        public bool IsContravariant =>
            (TypeParameterFlags & TypeParameterFlags.VarianceMask) == TypeParameterFlags.Contravariant;

        public override bool IsValueType => (TypeParameterFlags & TypeParameterFlags.ValueTypeConstraint) ==
                                            TypeParameterFlags.ValueTypeConstraint;

        public override bool IsStructural => true;

        /// <summary>True if the type serves as a parameter to a type template.</summary>
        public override bool IsTemplateParameter => true;

        public override bool IsReferenceType =>
            (TypeParameterFlags & TypeParameterFlags.ReferenceTypeConstraint) ==
            TypeParameterFlags.ReferenceTypeConstraint
            || (baseClass != null && baseClass != SystemTypes.Object && baseClass.IsReferenceType);
#if ExtendedRuntime
    private bool isUnmanagedIsValid = false;
    private bool isUnmanaged = false;
    public override bool IsUnmanaged{
      get{
        if (!isUnmanagedIsValid && SystemTypes.UnmanagedStructTemplateParameterAttribute != null){
          // check if we have the corresponding attribute
          for (int i = 0; i < (this.Attributes == null?0:this.Attributes.Count); i++){
            if (this.Attributes[i].Type == SystemTypes.UnmanagedStructTemplateParameterAttribute){
              isUnmanaged = true;
              break;
            }
          }
#if ExtendedRuntime
          if (!isUnmanaged) { isUnmanaged = IsPointerFree; }
#endif
          isUnmanagedIsValid = true;
        }
        return isUnmanaged;
      }
    }
    public void SetIsUnmanaged(){
      this.isUnmanaged = true;
      this.isUnmanagedIsValid = true;
    }
    private bool isPointerFreeIsValid = false;
    private bool isPointerFree = false;
    public override bool IsPointerFree
    {
      get
      {
        if (!isPointerFreeIsValid && SystemTypes.PointerFreeStructTemplateParameterAttribute != null)
        {
          // check if we have the corresponding attribute
          for (int i = 0; i < (this.Attributes == null ? 0 : this.Attributes.Count); i++)
          {
            if (this.Attributes[i].Type == SystemTypes.PointerFreeStructTemplateParameterAttribute)
            {
              isPointerFree = true;
              break;
            }
          }
          isPointerFreeIsValid = true;
        }
        return isPointerFree;
      }
    }
    public void SetIsPointerFree()
    {
      this.isPointerFree = true;
      this.isPointerFreeIsValid = true;
      // pointerfree implies Unmanaged
      SetIsUnmanaged();
    }
#endif
#if !NoXml
        public override XmlNode Documentation
        {
            get
            {
                if (documentation == null && DeclaringMember != null && Name != null)
                {
                    var parentDoc = DeclaringMember.Documentation;
                    if (parentDoc != null && parentDoc.HasChildNodes)
                    {
                        var myName = Name.Name;
                        foreach (XmlNode child in parentDoc.ChildNodes)
                            if (child.Name == "typeparam" && child.Attributes != null)
                                foreach (XmlAttribute attr in child.Attributes)
                                    if (attr != null && attr.Name == "name" && attr.Value == myName)
                                        return documentation = child;
                    }
                }

                return documentation;
            }
            set { documentation = value; }
        }

        public override string HelpText
        {
            get
            {
                if (helpText == null)
                {
                    var doc = Documentation;
                    if (doc != null) helpText = doc.InnerText;
                }

                return helpText;
            }
            set { helpText = value; }
        }
#endif
        protected internal TypeNodeList structuralElementTypes;
        public override TypeNodeList StructuralElementTypes
        {
            get
            {
                var result = structuralElementTypes;
                if (result != null) return result;
                structuralElementTypes = result = new TypeNodeList();
                if (BaseType != null) result.Add(BaseType);
                var interfaces = Interfaces;
                for (int i = 0, n = interfaces == null ? 0 : interfaces.Count; i < n; i++)
                {
                    var iface = interfaces[i];
                    if (iface == null) continue;
                    result.Add(iface);
                }

                return result;
            }
        }
#if !NoXml
        internal override void AppendDocumentIdMangledName(StringBuilder /*!*/ sb, TypeNodeList methodTypeParameters,
            TypeNodeList typeParameters)
        {
            if (TargetPlatform.GenericTypeNamesMangleChar != 0)
            {
                var n = methodTypeParameters == null ? 0 : methodTypeParameters.Count;
                for (var i = 0; i < n; i++)
                {
                    //^ assert methodTypeParameters != null;
                    var mpar = methodTypeParameters[i];
                    if (mpar != this) continue;
                    sb.Append(TargetPlatform.GenericTypeNamesMangleChar);
                    sb.Append(TargetPlatform.GenericTypeNamesMangleChar);
                    sb.Append(i);
                    return;
                }

                n = typeParameters == null ? 0 : typeParameters.Count;
                for (var i = 0; i < n; i++)
                {
                    var tpar = typeParameters[i];
                    if (tpar != this) continue;
                    sb.Append(TargetPlatform.GenericTypeNamesMangleChar);
                    sb.Append(i);
                    return;
                }

                sb.Append("not found:");
            }

            sb.Append(FullName);
        }
#endif
        public override string GetFullUnmangledNameWithoutTypeParameters()
        {
            return GetUnmangledNameWithoutTypeParameters();
        }

        public override string GetFullUnmangledNameWithTypeParameters()
        {
            return GetUnmangledNameWithTypeParameters();
        }

        public override bool IsStructurallyEquivalentTo(TypeNode type, Func<TypeNode, TypeNode> typeSubstitution = null)
        {
            if (null == (object)type) return false;
            if (this == type) return true;
            if (typeSubstitution != null && this == typeSubstitution(type)) return true;
            var itype = type as ITypeParameter;
            if (null == itype) return false;
            // if (type is MethodClassParameter || type is MethodTypeParameter) return false;
            if (Name != null && type.Name != null && Name.UniqueIdKey != type.Name.UniqueIdKey)
                if (DeclaringMember == itype.DeclaringMember)
                    return false;
            var bType = BaseType;
            var tbType = type.BaseType;
            if (null == (object)bType) bType = CoreSystemTypes.Object;
            if (null == (object)tbType) tbType = CoreSystemTypes.Object;
            if (bType != tbType /*&& !bType.IsStructurallyEquivalentTo(tbType)*/) return false;
            if (Interfaces == null) return type.Interfaces == null || type.Interfaces.Count == 0;
            if (type.Interfaces == null) return Interfaces.Count == 0;
            var n = Interfaces.Count;
            if (n != type.Interfaces.Count) return false;
            for (var i = 0; i < n; i++)
            {
                var i1 = Interfaces[i];
                var i2 = type.Interfaces[i];
                if (null == (object)i1 || null == (object)i2) return false;
                if (i1 != i2 /*&& !i1.IsStructurallyEquivalentTo(i2)*/) return false;
            }

            return true;
        }
#if !MinimalReader
        SourceContext ITypeParameter.SourceContext => SourceContext;
        Module ITypeParameter.DeclaringModule => DeclaringModule;
        TypeFlags ITypeParameter.Flags => Flags;
#endif
#if FxCop
    internal override void GetName(TypeFormat options, StringBuilder name)
    {
      if (options.TypeName == TypeNameFormat.FullyQualified)
      {
        TypeFormat typeFormat = options.Clone();
        typeFormat.TypeName = TypeNameFormat.Short;
        base.GetName(typeFormat, name);
        return;
      }
      base.GetName(options, name); 
    }
#endif
    }

    public class MethodClassParameter : ClassParameter
    {
        public MethodClassParameter()
        {
            NodeType = NodeType.ClassParameter;
            baseClass = CoreSystemTypes.Object;
            Flags = TypeFlags.NestedPublic | TypeFlags.Abstract;
            Namespace = StandardIds.TypeParameter;
        }
#if !NoReflection && WHIDBEY
        public override Type GetRuntimeType()
        {
            var m = DeclaringMember as Method;
            if (m == null) return null;
            var mi = m.GetMethodInfo();
            if (mi == null) return null;
            var typeParameters = mi.GetGenericArguments();
            if (ParameterListIndex >= typeParameters.Length) return null;
            return typeParameters[ParameterListIndex];
        }
#endif
#if !MinimalReader
        public override bool IsStructurallyEquivalentTo(TypeNode type, Func<TypeNode, TypeNode> typeSubstitution = null)
        {
            if (ReferenceEquals(this, type)) return true;
            if (typeSubstitution != null && this == typeSubstitution(type)) return true;
            var tp = type as ITypeParameter;
            if (tp == null) return false;
            if (type is MethodTypeParameter || type is MethodClassParameter)
                return ParameterListIndex == tp.ParameterListIndex;
            return base.IsStructurallyEquivalentTo(type as MethodClassParameter, typeSubstitution);
        }
#endif
    }

    public class ArrayType : TypeNode
    {
        internal ArrayType()
            : base(NodeType.ArrayType)
        {
        }

        internal ArrayType(TypeNode /*!*/ elementType, int rank)
            : this(elementType, rank, new int[0], new int[0])
        {
            if (rank == 1)
                typeCode = Metadata.ElementType.SzArray;
            else
                typeCode = Metadata.ElementType.Array;
        }

        internal ArrayType(TypeNode /*!*/ elementType, int rank, int[] sizes)
            : this(elementType, rank, sizes, new int[0])
        {
        }

        internal ArrayType(TypeNode /*!*/ elementType, int rank, int[] sizes, int[] lowerBounds)
            : base(null, null, null, elementType.Flags, null, null, null, null, NodeType.ArrayType)
        {
            Debug.Assert(elementType != null);
            Rank = rank;
            ElementType = elementType;
            DeclaringModule = elementType.DeclaringModule;
            LowerBounds = lowerBounds;
            Sizes = sizes;
            if (rank == 1)
                typeCode = Metadata.ElementType.SzArray;
            else
                typeCode = Metadata.ElementType.Array;
            if (elementType == null || elementType.Name == null) return;
            var name = new StringBuilder(ElementType.Name.ToString());
#if FxCop
      GetNameSuffix(name, false);
#else
            name.Append('[');
            var k = Sizes == null ? 0 : Sizes.Length;
            var m = LowerBounds == null ? 0 : LowerBounds.Length;
            for (int i = 0, n = Rank; i < n; i++)
            {
                if (i < k && Sizes[i] != 0)
                {
                    if (i < m && LowerBounds[i] != 0)
                    {
                        name.Append(LowerBounds[i]);
                        name.Append(':');
                    }

                    name.Append(Sizes[i]);
                }

                if (i < n - 1)
                    name.Append(',');
            }

            name.Append(']');
#endif
            Name = Identifier.For(name.ToString());
            Namespace = elementType.Namespace;
        }

        public TypeNode /*!*/ ElementType { get; set; }

        /// <summary>The interfaces implemented by this class or struct, or the extended by this interface.</summary>
        public override InterfaceList Interfaces
        {
            get
            {
                if (this.interfaces == null)
                {
                    var interfaces = new InterfaceList(SystemTypes.ICloneable, SystemTypes.IList,
                        SystemTypes.ICollection, SystemTypes.IEnumerable);
                    if (Rank == 1)
                        if (SystemTypes.GenericIEnumerable != null && SystemTypes.GenericIEnumerable.DeclaringModule ==
                            CoreSystemTypes.SystemAssembly)
                        {
                            interfaces.Add(
                                (Interface)SystemTypes.GenericIEnumerable.GetTemplateInstance(this, ElementType));
                            if (SystemTypes.GenericICollection != null)
                                interfaces.Add(
                                    (Interface)SystemTypes.GenericICollection.GetTemplateInstance(this, ElementType));
                            if (SystemTypes.GenericIList != null)
                                interfaces.Add(
                                    (Interface)SystemTypes.GenericIList.GetTemplateInstance(this, ElementType));
                        }

                    this.interfaces = interfaces;
                }

                return this.interfaces;
            }
            set { interfaces = value; }
        }

        public int Rank { get; set; }

        public int[] LowerBounds { get; set; }

        public int[] Sizes { get; set; }

        public bool IsSzArray()
        {
            return typeCode == Metadata.ElementType.SzArray;
        }

        private MemberList ctorList;
        private MemberList getterList;
        private MemberList setterList;
        private MemberList addressList;

        public override MemberList Members
        {
            get
            {
                if (this.members == null || membersBeingPopulated)
                    lock (this)
                    {
                        if (this.members == null)
                        {
                            membersBeingPopulated = true;
                            var members = this.members = new MemberList(5);
                            members.Add(Constructor);
                            //^ assume this.ctorList != null && this.ctorList.Length > 1;
                            members.Add(ctorList[1]);
                            members.Add(Getter);
                            members.Add(Setter);
                            members.Add(Address);
                            membersBeingPopulated = false;
                        }
                    }

                return this.members;
            }
            set { members = value; }
        }

        public override string /*!*/ FullName
        {
            get
            {
                if (ElementType != null && ElementType.DeclaringType != null)
                    return ElementType.DeclaringType.FullName + "+" + (Name == null ? "" : Name.ToString());
                if (Namespace != null && Namespace.UniqueIdKey != Identifier.Empty.UniqueIdKey)
                    return Namespace + "." + (Name == null ? "" : Name.ToString());
                if (Name != null)
                    return Name.ToString();
                return "";
            }
        }
#if !NoXml
        internal override void AppendDocumentIdMangledName(StringBuilder /*!*/ sb, TypeNodeList methodTypeParameters,
            TypeNodeList typeParameters)
        {
            if (ElementType == null) return;
            ElementType.AppendDocumentIdMangledName(sb, methodTypeParameters, typeParameters);
            sb.Append('[');
            var k = Sizes == null ? 0 : Sizes.Length;
            var m = LowerBounds == null ? 0 : LowerBounds.Length;
            for (int i = 0, n = Rank; i < n; i++)
            {
                if (i < k && Sizes[i] != 0)
                {
                    if (i < m && LowerBounds[i] != 0)
                    {
                        sb.Append(LowerBounds[i]);
                        sb.Append(':');
                    }

                    sb.Append(Sizes[i]);
                }

                if (i < n - 1)
                    sb.Append(',');
            }

            sb.Append(']');
        }
#endif
        public virtual void SetLowerBoundToUnknown()
        {
            CC.Contract.Assume(Rank == 1);
            typeCode = Metadata.ElementType.Array;
        }

        public virtual int GetLowerBound(int dimension)
        {
            if (LowerBounds == null || LowerBounds.Length <= dimension) return 0;
            return LowerBounds[dimension];
        }

        public virtual int GetSize(int dimension)
        {
            if (Sizes == null || Sizes.Length <= dimension) return 0;
            return Sizes[dimension];
        }

        public override MemberList /*!*/ GetMembersNamed(Identifier name)
        {
            if (name == null) return new MemberList(0);
            if (name.UniqueIdKey == StandardIds.Get.UniqueIdKey)
            {
                if (getterList == null)
                {
                    var getter = Getter;
                    if (getter != null) getter = null;
                    //^ assume this.getterList != null;
                }

                return getterList;
            }

            if (name.UniqueIdKey == StandardIds.Set.UniqueIdKey)
            {
                if (setterList == null)
                {
                    var setter = Setter;
                    if (setter != null) setter = null;
                    //^ assume this.setterList != null;
                }

                return setterList;
            }

            if (name.UniqueIdKey == StandardIds.Ctor.UniqueIdKey)
            {
                if (ctorList == null)
                {
                    var ctor = Constructor;
                    if (ctor != null) ctor = null;
                    //^ assume this.ctorList != null;
                }

                return ctorList;
            }

            if (name.UniqueIdKey == StandardIds.Address.UniqueIdKey)
            {
                if (addressList == null)
                {
                    var addr = Address;
                    if (addr != null) addr = null;
                    //^ assume this.addressList != null;
                }

                return addressList;
            }

            return new MemberList(0);
        }
#if !NoReflection
        public override Type GetRuntimeType()
        {
            if (runtimeType == null)
            {
                if (ElementType == null) return null;
                var eType = ElementType.GetRuntimeType();
                if (eType == null) return null;
#if WHIDBEY
                if (IsSzArray())
                    runtimeType = eType.MakeArrayType();
                else
                    runtimeType = eType.MakeArrayType(Rank);
#else
        StringBuilder sb = new StringBuilder(eType.FullName);
        sb.Append('[');
        for (int i = 1, n = this.Rank; i < n; i++) sb.Append(',');
        sb.Append(']');
        if (eType.Assembly != null)
          this.runtimeType = eType.Assembly.GetType(sb.ToString(), false);
        else if (eType.Module != null)
          this.runtimeType = eType.Module.GetType(sb.ToString(), false);
#endif
            }

            return runtimeType;
        }
#endif
        public Method Constructor
        {
            get
            {
                if (ctorList == null)
                    lock (this)
                    {
                        if (ctorList == null)
                        {
                            var ctor = new InstanceInitializer();
                            ctor.DeclaringType = this;
                            ctor.Flags |= MethodFlags.Public;
                            var n = Rank;
                            ctor.Parameters = new ParameterList(n);
                            for (var i = 0; i < n; i++)
                            {
                                var par = new Parameter();
                                par.DeclaringMethod = ctor;
                                par.Type = CoreSystemTypes.Int32;
                                ctor.Parameters.Add(par);
                            }

                            ctorList = new MemberList(2);
                            ctorList.Add(ctor);
                            ctor = new InstanceInitializer();
                            ctor.DeclaringType = this;
                            ctor.Flags |= MethodFlags.Public;
                            ctor.Parameters = new ParameterList(n = n * 2);
                            for (var i = 0; i < n; i++)
                            {
                                var par = new Parameter();
                                par.Type = CoreSystemTypes.Int32;
                                par.DeclaringMethod = ctor;
                                ctor.Parameters.Add(par);
                            }

                            ctorList.Add(ctor);
                        }
                    }

                return (Method)ctorList[0];
            }
        }

        public Method Getter
        {
            get
            {
                if (getterList == null)
                    lock (this)
                    {
                        if (getterList == null)
                        {
                            var getter = new Method();
                            getter.Name = StandardIds.Get;
                            getter.DeclaringType = this;
                            getter.CallingConvention = CallingConventionFlags.HasThis;
                            getter.Flags = MethodFlags.Public;
                            getter.Parameters = new ParameterList();
                            for (int i = 0, n = Rank; i < n; i++)
                            {
                                var par = new Parameter();
                                par.Type = CoreSystemTypes.Int32;
                                par.DeclaringMethod = getter;
                                getter.Parameters.Add(par);
                            }

                            getter.ReturnType = ElementType;
                            getterList = new MemberList();
                            getterList.Add(getter);
                        }
                    }

                return (Method)getterList[0];
            }
        }

        public Method Setter
        {
            get
            {
                if (setterList == null)
                    lock (this)
                    {
                        if (setterList == null)
                        {
                            var setter = new Method();
                            setter.Name = StandardIds.Set;
                            setter.DeclaringType = this;
                            setter.CallingConvention = CallingConventionFlags.HasThis;
                            setter.Flags = MethodFlags.Public;
                            setter.Parameters = new ParameterList();
                            Parameter par;
                            for (int i = 0, n = Rank; i < n; i++)
                            {
                                par = new Parameter();
                                par.Type = CoreSystemTypes.Int32;
                                par.DeclaringMethod = setter;
                                setter.Parameters.Add(par);
                            }

                            par = new Parameter();
                            par.Type = ElementType;
                            par.DeclaringMethod = setter;
                            setter.Parameters.Add(par);
                            setter.ReturnType = CoreSystemTypes.Void;
                            setterList = new MemberList();
                            setterList.Add(setter);
                        }
                    }

                return (Method)setterList[0];
            }
        }

        public Method Address
        {
            get
            {
                if (addressList == null)
                    lock (this)
                    {
                        if (addressList == null)
                        {
                            var address = new Method();
                            address.Name = StandardIds.Address;
                            address.DeclaringType = this;
                            address.CallingConvention = CallingConventionFlags.HasThis;
                            address.Flags = MethodFlags.Public;
                            address.Parameters = new ParameterList();
                            for (int i = 0, n = Rank; i < n; i++)
                            {
                                var par = new Parameter();
                                par.Type = CoreSystemTypes.Int32;
                                par.DeclaringMethod = address;
                                address.Parameters.Add(par);
                            }

                            address.ReturnType = ElementType.GetReferenceType();
                            addressList = new MemberList();
                            addressList.Add(address);
                        }
                    }

                return (Method)addressList[0];
            }
        }

        public override bool IsAssignableTo(TypeNode targetType, Func<TypeNode, TypeNode> targetTypeSubstitution = null)
        {
            if (targetType == null) return false;
            if (targetType == this || targetType == CoreSystemTypes.Object || targetType == CoreSystemTypes.Array ||
                targetType == SystemTypes.ICloneable) return true;
            if (CoreSystemTypes.Array.IsAssignableTo(targetType, targetTypeSubstitution)) return true;
            if (targetType.Template != null && SystemTypes.GenericIEnumerable != null &&
                SystemTypes.GenericIEnumerable.DeclaringModule == CoreSystemTypes.SystemAssembly)
                if (targetType.Template == SystemTypes.GenericIEnumerable ||
                    targetType.Template == SystemTypes.GenericICollection ||
                    targetType.Template == SystemTypes.GenericIList)
                {
                    if (targetType.TemplateArguments == null || targetType.TemplateArguments.Count != 1)
                    {
                        Debug.Assert(false);
                        return false;
                    }

                    var ienumElementType = targetType.TemplateArguments[0];
                    if (ElementType == ienumElementType) return true;
                    if (ElementType.IsValueType) return false;
                    return ElementType.IsAssignableTo(ienumElementType, targetTypeSubstitution);
                }

            var targetArrayType = targetType as ArrayType;
            if (targetArrayType == null) return false;
            if (Rank != 1 || targetArrayType.Rank != 1) return false;
            var thisElementType = ElementType;
            if (thisElementType == null) return false;
#if ExtendedRuntime
      thisElementType = TypeNode.StripModifier(thisElementType, ExtendedRuntimeTypes.NonNullType);
      // DelayedAttribute is used as a modifier on some array allocation types to mark it as 
      // an explictly delayed allocation.
      thisElementType = TypeNode.StripModifier(thisElementType, ExtendedRuntimeTypes.DelayedAttribute);
#endif
            if (thisElementType == targetArrayType.ElementType) return true;
            if (thisElementType.IsValueType) return false;
            return thisElementType.IsAssignableTo(targetArrayType.ElementType, targetTypeSubstitution);
        }

        public override bool IsStructural => true;
        protected TypeNodeList structuralElementTypes;

        public override TypeNodeList StructuralElementTypes
        {
            get
            {
                var result = structuralElementTypes;
                if (result != null) return result;
                structuralElementTypes = result = new TypeNodeList(1);
                result.Add(ElementType);
                return result;
            }
        }

        public override bool IsStructurallyEquivalentTo(TypeNode type, Func<TypeNode, TypeNode> typeSubstitution = null)
        {
            if (type == null) return false;
            if (this == type) return true;
            if (typeSubstitution != null && this == typeSubstitution(type)) return true;
            var t = type as ArrayType;
            if (t == null) return false;
            if (Rank != t.Rank) return false;
            if (ElementType == null || t.ElementType == null) return false;
            if (ElementType != t.ElementType &&
                !ElementType.IsStructurallyEquivalentTo(t.ElementType, typeSubstitution)) return false;
            if (Sizes == null) return t.Sizes == null;
            if (t.Sizes == null) return false;
            var n = Sizes.Length;
            if (n != t.Sizes.Length) return false;
            for (var i = 0; i < n; i++)
                if (Sizes[i] != t.Sizes[i])
                    return false;
            if (LowerBounds == null) return t.LowerBounds == null;
            if (t.LowerBounds == null) return false;
            n = LowerBounds.Length;
            if (n != t.LowerBounds.Length) return false;
            for (var i = 0; i < n; i++)
                if (LowerBounds[i] != t.LowerBounds[i])
                    return false;
            return true;
        }
#if FxCop
    internal override void GetName(MemberFormat options, StringBuilder name)
    {
      this.ElementType.GetName(options, name);
      GetNameSuffix(name, options.InsertSpacesBetweenMethodTypeParameters);
    }
    private void GetNameSuffix(StringBuilder name, bool insertSpacesBetweenParameters)
    {
      name.Append('[');
      int k = this.Sizes == null ? 0 : this.Sizes.Length;
      int m = this.LowerBounds == null ? 0 : this.LowerBounds.Length;
      for (int i = 0, n = this.Rank; i < n; i++)
      {
        if (i < k && this.Sizes[i] != 0)
        {
          if (i < m && this.LowerBounds[i] != 0)
          {
            name.Append(this.LowerBounds[i].ToString("0", CultureInfo.InvariantCulture));
            name.Append(':');
          }
          name.Append(this.Sizes[i].ToString("0", CultureInfo.InvariantCulture));
        }
        if (i < n - 1)
        {
          name.Append(',');
          if (insertSpacesBetweenParameters)
            name.Append(' ');
        }
      }
      name.Append(']');
    }
#endif
    }

    public class Pointer : TypeNode
    {
        internal Pointer(TypeNode /*!*/ elementType)
            : base(NodeType.Pointer)
        {
            ElementType = elementType;
            typeCode = Metadata.ElementType.Pointer;
            Name = Identifier.For(elementType.Name + "*");
            Namespace = elementType.Namespace;
        }

        public TypeNode /*!*/ ElementType { get; set; }

        public override string /*!*/ FullName
        {
            get
            {
                if (ElementType != null && ElementType.DeclaringType != null)
                    return ElementType.DeclaringType.FullName + "+" + (Name == null ? "" : Name.ToString());
                if (Namespace != null && Namespace.UniqueIdKey != Identifier.Empty.UniqueIdKey)
                    return Namespace + "." + (Name == null ? "" : Name.ToString());
                if (Name != null)
                    return Name.ToString();
                return "";
            }
        }
#if !NoXml
        internal override void AppendDocumentIdMangledName(StringBuilder /*!*/ sb, TypeNodeList methodTypeParameters,
            TypeNodeList typeParameters)
        {
            if (ElementType == null) return;
            ElementType.AppendDocumentIdMangledName(sb, methodTypeParameters, typeParameters);
            sb.Append('*');
        }
#endif
#if !NoReflection
        public override Type GetRuntimeType()
        {
            if (runtimeType == null)
            {
                if (ElementType == null) return null;
                var eType = ElementType.GetRuntimeType();
                if (eType == null) return null;
#if WHIDBEY
                runtimeType = eType.MakePointerType();
#else
        if (eType.Assembly != null)
          this.runtimeType = eType.Assembly.GetType(eType.FullName+"*", false);
        else
          this.runtimeType = eType.Module.GetType(eType.FullName+"*", false);
#endif
            }

            return runtimeType;
        }
#endif
        public override bool IsAssignableTo(TypeNode targetType, Func<TypeNode, TypeNode> targetTypeSubstitution = null)
        {
            if (targetType == this) return true;
            if (targetTypeSubstitution != null && this == targetTypeSubstitution(targetType)) return true;
            var tp = targetType as Pointer;
            if (tp == null) return false;
            if (tp.ElementType == CoreSystemTypes.Void) return true;
            if (ElementType == null || tp.ElementType == null) return false;
            return ElementType.IsStructurallyEquivalentTo(tp.ElementType, targetTypeSubstitution);
        }

        public override bool IsUnmanaged => true;

        public override bool IsStructural => true;

        public override bool IsPointerType => true;

        protected TypeNodeList structuralElementTypes;

        public override TypeNodeList StructuralElementTypes
        {
            get
            {
                var result = structuralElementTypes;
                if (result != null) return result;
                structuralElementTypes = result = new TypeNodeList(1);
                result.Add(ElementType);
                return result;
            }
        }

        public override bool IsStructurallyEquivalentTo(TypeNode type, Func<TypeNode, TypeNode> typeSubstitution = null)
        {
            if (type == null) return false;
            if (this == type) return true;
            if (typeSubstitution != null && this == typeSubstitution(type)) return true;
            var t = type as Pointer;
            if (t == null) return false;
            if (ElementType == null || t.ElementType == null) return false;
            return ElementType == t.ElementType ||
                   ElementType.IsStructurallyEquivalentTo(t.ElementType, typeSubstitution);
        }
#if FxCop
    internal override void GetName(TypeFormat options, StringBuilder name)
    {
      this.ElementType.GetName(options, name);
      name.Append('*');
    }
#endif
    }

    public class Reference : TypeNode
    {
        internal Reference(TypeNode /*!*/ elementType)
            : base(NodeType.Reference)
        {
            ElementType = elementType;
            typeCode = Metadata.ElementType.Reference;
            Name = Identifier.For(elementType.Name + "@");
            Namespace = elementType.Namespace;
        }

        public TypeNode /*!*/ ElementType { get; set; }
#if !NoXml
        internal override void AppendDocumentIdMangledName(StringBuilder /*!*/ sb, TypeNodeList methodTypeParameters,
            TypeNodeList typeParameters)
        {
            if (ElementType == null) return;
            ElementType.AppendDocumentIdMangledName(sb, methodTypeParameters, typeParameters);
            sb.Append('@');
        }
#endif
        public override bool IsAssignableTo(TypeNode targetType, Func<TypeNode, TypeNode> targetTypeSubstitution = null)
        {
            if (targetType == this) return true;
            if (targetTypeSubstitution != null && this == targetTypeSubstitution(targetType)) return true;
            var tr = targetType as Reference;
            if (tr != null) return ElementType.IsStructurallyEquivalentTo(tr.ElementType, targetTypeSubstitution);
            var tp = targetType as Pointer;
            if (tp != null)
                return tp.ElementType == CoreSystemTypes.Void ||
                       ElementType.IsStructurallyEquivalentTo(tp.ElementType, targetTypeSubstitution);
            return false;
        }

        public override string /*!*/ FullName
        {
            get
            {
                if (ElementType != null && ElementType.DeclaringType != null)
                    return ElementType.DeclaringType.FullName + "+" + (Name == null ? "" : Name.ToString());
                if (Namespace != null && Namespace.UniqueIdKey != Identifier.Empty.UniqueIdKey)
                    return Namespace + "." + (Name == null ? "" : Name.ToString());
                if (Name != null)
                    return Name.ToString();
                return "";
            }
        }
#if !NoReflection
        public override Type GetRuntimeType()
        {
            if (runtimeType == null)
            {
                if (ElementType == null) return null;
                var eType = ElementType.GetRuntimeType();
                if (eType == null) return null;
#if WHIDBEY
                runtimeType = eType.MakeByRefType();
#else
        if (eType.Assembly != null)
          this.runtimeType = eType.Assembly.GetType(eType.FullName+"&", false);
        else
          this.runtimeType = eType.Module.GetType(eType.FullName+"&", false);
#endif
            }

            return runtimeType;
        }
#endif
        public override bool IsStructural => true;
        protected TypeNodeList structuralElementTypes;

        public override TypeNodeList StructuralElementTypes
        {
            get
            {
                var result = structuralElementTypes;
                if (result != null) return result;
                structuralElementTypes = result = new TypeNodeList(1);
                result.Add(ElementType);
                return result;
            }
        }

        public override bool IsStructurallyEquivalentTo(TypeNode type, Func<TypeNode, TypeNode> typeSubstitution = null)
        {
            if (type == null) return false;
            if (this == type) return true;
            if (typeSubstitution != null && this == typeSubstitution(type)) return true;
            var t = type as Reference;
            if (t == null) return false;
            if (ElementType == null || t.ElementType == null) return false;
            return ElementType == t.ElementType ||
                   ElementType.IsStructurallyEquivalentTo(t.ElementType, typeSubstitution);
        }
#if FxCop
    internal override void GetName(TypeFormat options, StringBuilder name)
    {
      this.ElementType.GetName(options, name);
      name.Append('&');
    }
#endif
    }
#if ExtendedRuntime
  public class TupleType : Struct{
    private TupleType(FieldList domains, Identifier/*!*/ name, TypeNode/*!*/ referringType, TypeFlags visibility) {
      referringType.DeclaringModule.StructurallyEquivalentType[name.UniqueIdKey] = this;
      this.DeclaringModule = referringType.DeclaringModule;
      this.NodeType = NodeType.TupleType;
      this.Flags = TypeFlags.Sealed;
      this.Namespace = StandardIds.StructuralTypes;
      this.Name = name;
      this.isNormalized = true;
      switch (visibility){
        case TypeFlags.NestedFamANDAssem:
        case TypeFlags.NestedFamily:
        case TypeFlags.NestedPrivate:
          referringType.Members.Add(this);
          this.DeclaringType = referringType;
          this.Flags |= TypeFlags.NestedPrivate;
          break;
        default:
          referringType.DeclaringModule.Types.Add(this);
          this.Flags |= TypeFlags.Public;
          break;
      }
      int n = domains == null ? 0 : domains.Count;
      MemberList members = this.members = new MemberList(n);
      TypeNodeList types = new TypeNodeList(n);
      for (int i = 0; i < n; i++){
        //^ assert domains != null;
        Field f = domains[i];
        if (f == null) continue;
        f = (Field)f.Clone();
        f.DeclaringType = this;
        members.Add(f);
        if (f.Type != null)
          types.Add(f.Type);       
      }
      TypeNode elemType = null;
      if (n == 1)
        elemType = types[0]; //TODO: get element type of stream?
      else{
        TypeUnion tu = TypeUnion.For(types, referringType);
        //^ assume tu != null;
        elemType = tu;
        if (tu.Types.Count == 1) elemType = tu.Types[0];
      }
      if (elemType == null) elemType = CoreSystemTypes.Object;
      Interface ienumerable = (Interface)SystemTypes.GenericIEnumerable.GetTemplateInstance(referringType, elemType);
      Interface ienumerator = (Interface)SystemTypes.GenericIEnumerator.GetTemplateInstance(referringType, elemType);
      this.Interfaces = new InterfaceList(SystemTypes.TupleType, ienumerable, SystemTypes.IEnumerable);

      This ThisParameter = new This(this.GetReferenceType());
      StatementList statements = new StatementList(1);
      TypeNode tEnumerator = TupleEnumerator.For(this, n, elemType, ienumerator, referringType);
      InstanceInitializer cons = tEnumerator.GetConstructor(this);
      if (cons == null) { Debug.Fail(""); return; }
      ExpressionList args = new ExpressionList(new AddressDereference(ThisParameter, this));
      statements.Add(new Return(new Construct(new MemberBinding(null, cons), args)));
      Block body = new Block(statements);
      Method getEnumerator = new Method(this, null, StandardIds.GetEnumerator, null, ienumerator, body);
      getEnumerator.Flags = MethodFlags.Public|MethodFlags.Virtual;
      getEnumerator.CallingConvention = CallingConventionFlags.HasThis;
      getEnumerator.ThisParameter = ThisParameter;
      this.members.Add(getEnumerator);

      //IEnumerable.GetEnumerator
      ThisParameter = new This(this.GetReferenceType());
      statements = new StatementList(1);
      MethodCall mcall =
 new MethodCall(new MemberBinding(ThisParameter, getEnumerator), new ExpressionList(0), NodeType.Call, SystemTypes.IEnumerator);
      statements.Add(new Return(mcall));
      getEnumerator =
 new Method(this, null, StandardIds.IEnumerableGetEnumerator, null, SystemTypes.IEnumerator, new Block(statements));
      getEnumerator.ThisParameter = ThisParameter;
      getEnumerator.ImplementedInterfaceMethods =
 new MethodList(SystemTypes.IEnumerable.GetMethod(StandardIds.GetEnumerator));
      getEnumerator.CallingConvention = CallingConventionFlags.HasThis;
      getEnumerator.Flags = MethodFlags.Private | MethodFlags.Virtual | MethodFlags.SpecialName;
      this.members.Add(getEnumerator);
    }
    internal TupleType(NestedTypeProvider provideNestedTypes, TypeAttributeProvider provideAttributes, TypeMemberProvider provideMembers, object handle)
      : base(provideNestedTypes, provideAttributes, provideMembers, handle) {
      this.NodeType = NodeType.TupleType;
      this.typeCode = ElementType.ValueType;
    }
    public static TupleType For(FieldList domains, TypeNode referringType){
      if (referringType == null) return null;
      Module module = referringType.DeclaringModule;
      if (module == null) return null;
      TypeFlags visibility = TypeFlags.Public;
      StringBuilder name = new StringBuilder();
      name.Append("Tuple");
      int n = domains == null ? 0 : domains.Count;
      for (int i = 0; i < n; i++) {
        //^ assert domains != null;
        Field f = domains[i];
        if (f == null || f.Type == null || f.Type.Name == null) continue;
        visibility = TypeNode.GetVisibilityIntersection(visibility, f.Type.Flags & TypeFlags.VisibilityMask);
        name.Append('_');
        name.Append(f.Type.Name.ToString());
        if (f.Name != null && !f.IsSpecialName) {
          name.Append('_');
          name.Append(f.Name.ToString());
        }
      }
      TupleType tup = null;
      int tCount = 0;
      string tNameString = name.ToString();
      Identifier tName = Identifier.For(tNameString);
      TypeNode result = module.GetStructurallyEquivalentType(StandardIds.StructuralTypes, tName);
      while (result != null) {
        //Mangled name is the same. But mangling is not unique (types are not qualified with assemblies), so check for equality.
        tup = result as TupleType;
        bool goodMatch = tup != null;
        if (goodMatch) {
          //^ assert tup != null;
          MemberList tMembers = tup.Members;
          int m = tMembers == null ? 0 : tMembers.Count;
          goodMatch = n == m-2;
          if (goodMatch) {
            //^ assert domains != null;
            //^ assert tMembers != null;
            for (int i = 0; goodMatch && i < n; i++) {
              Field f1 = domains[i];
              Field f2 = tMembers[i] as Field;
              goodMatch = f1 != null && f2 != null && f1.Type == f2.Type && 
              f1.Name != null && f2.Name != null && f1.Name.UniqueIdKey == f2.Name.UniqueIdKey;
            }
          }
        }
        if (goodMatch) return tup;
        //Mangle some more
        tName = Identifier.For(tNameString+(++tCount).ToString());
        result = module.GetStructurallyEquivalentType(StandardIds.StructuralTypes, tName);
      }
      tup = new TupleType(domains, tName, referringType, visibility);
      return tup;
    }
    public override bool IsStructural{
      get{return true;}
    }
    protected TypeNodeList structuralElementTypes;
    public override TypeNodeList StructuralElementTypes{
      get{
        TypeNodeList result = this.structuralElementTypes;
        if (result != null) return result;
        this.structuralElementTypes = result = new TypeNodeList(1);
        MemberList members = this.Members;
        for (int i = 0, n = members == null ? 0 : members.Count; i < n; i++){
          Field f = members[i] as Field;
          if (f == null || f.Type == null) continue;
          result.Add(f.Type);
        }
        return result;
      }
    }
    public override bool IsStructurallyEquivalentTo(TypeNode type){
      if (type == null) return false;
      if (this == type) return true;
      TupleType t = type as TupleType;
      if (t == null) return false;
      if (this.Members == null) return t.Members == null;
      if (t.Members == null) return false;
      int n = this.Members.Count; if (n != t.Members.Count) return false;
      for (int i = 0; i < n; i++){
        Member m1 = this.Members[i];
        Member m2 = t.Members[i];
        if (m1 == null || m2 == null) return false;
        Field f1 = m1 as Field;
        Field f2 = m2 as Field;
        if (f1 == null && f2 == null) continue;
        if (f1 == null || f2 == null) return false;
        if (f1.Name == null || f2.Name == null) return false;
        if (f1.Type == null || f2.Type == null) return false;
        if (f1.Name.UniqueIdKey != f2.Name.UniqueIdKey) return false;
        if (f1.Type != f2.Type && !f1.Type.IsStructurallyEquivalentTo(f2.Type)) return false;
      }
      return true;
    }
  }
  internal sealed class TupleEnumerator{
    private TupleEnumerator(){}
    internal static TypeNode/*!*/ For(TupleType/*!*/ tuple, int numDomains, TypeNode/*!*/ elementType, Interface/*!*/ targetIEnumerator, TypeNode/*!*/ referringType) {
      Identifier id = Identifier.For("Enumerator"+tuple.Name);
      InterfaceList interfaces = new InterfaceList(targetIEnumerator, SystemTypes.IDisposable, SystemTypes.IEnumerator);
      MemberList members = new MemberList(5);
      Class enumerator =
 new Class(referringType.DeclaringModule, null, null, TypeFlags.Sealed, targetIEnumerator.Namespace, id, CoreSystemTypes.Object, interfaces, members);
      enumerator.IsNormalized = true;
      if ((tuple.Flags & TypeFlags.VisibilityMask) == TypeFlags.Public){
        enumerator.Flags |= TypeFlags.Public;
        referringType.DeclaringModule.Types.Add(enumerator);
      }else{
        enumerator.Flags |= TypeFlags.NestedPrivate;
        referringType.Members.Add(enumerator);
        enumerator.DeclaringType = referringType;
      }
      //Field to hold tuple
      Field tField = new Field(enumerator, null, FieldFlags.Private, StandardIds.Value, tuple, null);
      members.Add(tField);
      //Field to hold current position
      Field pField = new Field(enumerator, null, FieldFlags.Private, StandardIds.Position, CoreSystemTypes.Int32, null);
      members.Add(pField);
      //Constructor
      Parameter par = new Parameter(null, ParameterFlags.None, StandardIds.Value, tuple, null, null);
      StatementList statements = new StatementList(4);
      InstanceInitializer constr = CoreSystemTypes.Object.GetConstructor();
      if (constr == null) { Debug.Fail(""); return enumerator; }
      This thisParameter = new This(enumerator);
      MethodCall mcall =
 new MethodCall(new MemberBinding(thisParameter, constr), new ExpressionList(0), NodeType.Call, CoreSystemTypes.Void);
      statements.Add(new ExpressionStatement(mcall));
      statements.Add(new AssignmentStatement(new MemberBinding(thisParameter, tField), par));
      statements.Add(new AssignmentStatement(new MemberBinding(thisParameter, pField), Literal.Int32MinusOne));
      statements.Add(new Return());
      InstanceInitializer econs =
 new InstanceInitializer(enumerator, null, new ParameterList(par), new Block(statements));
      econs.ThisParameter = thisParameter;
      econs.Flags |= MethodFlags.Public;
      members.Add(econs);
      //get_Current
      thisParameter = new This(enumerator);
      statements = new StatementList(numDomains+1);
      BlockList blocks = new BlockList(numDomains);
      statements.Add(new SwitchInstruction(new MemberBinding(thisParameter, pField), blocks));
      constr = SystemTypes.InvalidOperationException.GetConstructor();
      if (constr == null) { Debug.Fail(""); return enumerator; }
      statements.Add(new Throw(new Construct(new MemberBinding(null, constr), null)));
      for (int i = 0; i < numDomains; i++){
        Field f = (Field)tuple.members[i];
        MemberBinding mb =
 new MemberBinding(new UnaryExpression(new MemberBinding(thisParameter, tField), NodeType.AddressOf), f);
        Block b = new Block();
        statements.Add(b);
        blocks.Add(b);
        if (f.Type == elementType || f.Type == null)
          b.Statements = new StatementList(new Return(mb));
        else{
          TypeUnion tUnion = elementType as TypeUnion;
          Debug.Assert(tUnion != null);
          if (tUnion != null){
            Method m = tUnion.GetImplicitCoercionFromMethod(f.Type);
            if (m != null){
              MethodCall mCall = new MethodCall(new MemberBinding(null, m), new ExpressionList(mb));
              b.Statements = new StatementList(new Return(mCall));
            }else{
              TypeUnion eUnion = f.Type as TypeUnion;
              if (eUnion != null){
                Method getTagAsType = eUnion.GetMethod(StandardIds.GetTagAsType);
                Method getValue = eUnion.GetMethod(StandardIds.GetValue);
                Method fromObject =
 tUnion.GetMethod(StandardIds.FromObject, CoreSystemTypes.Object, CoreSystemTypes.Type);
                if (getTagAsType == null || getValue == null || fromObject == null) {
                  Debug.Fail(""); return enumerator;
                }
                Local temp = new Local(Identifier.Empty, eUnion);
                Expression tempAddr = new UnaryExpression(temp, NodeType.AddressOf);
                StatementList stats = new StatementList(2);
                stats.Add(new AssignmentStatement(temp, mb));
                ExpressionList arguments = new ExpressionList(2);
                arguments.Add(new MethodCall(new MemberBinding(tempAddr, getValue), null));
                arguments.Add(new MethodCall(new MemberBinding(tempAddr, getTagAsType), null));
                stats.Add(new Return(new MethodCall(new MemberBinding(null, fromObject), arguments)));
                b.Statements = stats;
              }else{
                Debug.Assert(false);
              }
            }
          }
        }
      }
      Method getCurrent =
 new Method(enumerator, null, StandardIds.getCurrent, null, elementType, new Block(statements));
      getCurrent.Flags =
 MethodFlags.Public|MethodFlags.Virtual|MethodFlags.NewSlot|MethodFlags.HideBySig|MethodFlags.SpecialName;
      getCurrent.CallingConvention = CallingConventionFlags.HasThis;
      getCurrent.ThisParameter = thisParameter;
      members.Add(getCurrent);

      //IEnumerator.GetCurrent
      statements = new StatementList(1);
      This ThisParameter = new This(enumerator);
      MethodCall callGetCurrent =
 new MethodCall(new MemberBinding(ThisParameter, getCurrent), new ExpressionList(0), NodeType.Call, elementType); 
      MemberBinding etExpr = new MemberBinding(null, elementType);
      statements.Add(new Return(new BinaryExpression(callGetCurrent, etExpr, NodeType.Box, CoreSystemTypes.Object)));
      Method ieGetCurrent =
 new Method(enumerator, null, StandardIds.IEnumeratorGetCurrent, null, CoreSystemTypes.Object, new Block(statements));
      ieGetCurrent.ThisParameter = ThisParameter;
      ieGetCurrent.ImplementedInterfaceMethods =
 new MethodList(SystemTypes.IEnumerator.GetMethod(StandardIds.getCurrent));
      ieGetCurrent.CallingConvention = CallingConventionFlags.HasThis;
      ieGetCurrent.Flags = MethodFlags.Private|MethodFlags.Virtual|MethodFlags.SpecialName;
      members.Add(ieGetCurrent);

      //IEnumerator.Reset
      statements = new StatementList(2);
      ThisParameter = new This(enumerator);
      statements.Add(new AssignmentStatement(new MemberBinding(ThisParameter, pField), Literal.Int32Zero));
      statements.Add(new Return());
      Method reset =
 new Method(enumerator, null, StandardIds.IEnumeratorReset, null, CoreSystemTypes.Void, new Block(statements));
      reset.ThisParameter = ThisParameter;
      reset.ImplementedInterfaceMethods = new MethodList(SystemTypes.IEnumerator.GetMethod(StandardIds.Reset));
      reset.CallingConvention = CallingConventionFlags.HasThis;
      reset.Flags = MethodFlags.Private|MethodFlags.Virtual|MethodFlags.SpecialName;
      members.Add(reset);

      //MoveNext
      ThisParameter = new This(enumerator);
      statements = new StatementList(5);
      MemberBinding pos = new MemberBinding(ThisParameter, pField);
      Expression comparison = new BinaryExpression(pos, new Literal(numDomains, CoreSystemTypes.Int32), NodeType.Lt);
      Block returnTrue = new Block();
      statements.Add(new AssignmentStatement(pos, new BinaryExpression(pos, Literal.Int32One, NodeType.Add)));
      statements.Add(new Branch(comparison, returnTrue));
      statements.Add(new Return(Literal.False));
      statements.Add(returnTrue);
      statements.Add(new Return(Literal.True));
      Method moveNext =
 new Method(enumerator, null, StandardIds.MoveNext, null, CoreSystemTypes.Boolean, new Block(statements));
      moveNext.Flags = MethodFlags.Public|MethodFlags.Virtual|MethodFlags.NewSlot|MethodFlags.HideBySig;
      moveNext.CallingConvention = CallingConventionFlags.HasThis;
      moveNext.ThisParameter = ThisParameter;
      members.Add(moveNext);
      //IDispose.Dispose
      statements = new StatementList(1);
      statements.Add(new Return());
      Method dispose =
 new Method(enumerator, null, StandardIds.Dispose, null, CoreSystemTypes.Void, new Block(statements));
      dispose.CallingConvention = CallingConventionFlags.HasThis;
      dispose.Flags = MethodFlags.Public|MethodFlags.Virtual;
      enumerator.Members.Add(dispose);
      return enumerator;
    }
  }
  public class TypeAlias : Struct{
    protected TypeNode aliasedType;
    public TypeNode AliasedTypeExpression;
    public bool RequireExplicitCoercionFromUnderlyingType;
    public TypeAlias()
      : this(null, null){
    }
    internal TypeAlias(NestedTypeProvider provideNestedTypes, TypeAttributeProvider provideAttributes, TypeMemberProvider provideMembers, object handle, bool requireExplicitCoercionFromUnderlyingType)
      : base(provideNestedTypes, provideAttributes, provideMembers, handle) {
      this.RequireExplicitCoercionFromUnderlyingType = requireExplicitCoercionFromUnderlyingType;
    }
    public TypeAlias(TypeNode aliasedType, Identifier name)
      : base(){
      this.NodeType = NodeType.TypeAlias;
      this.AliasedType = aliasedType;
      this.Name = name;
    }
    public TypeNode AliasedType{
      get{
        if (this.aliasedType == null){
          Field f = this.GetField(StandardIds.Value);
          if (f != null)
            this.aliasedType = f.Type;
        }
        return this.aliasedType;
      }
      set{
        this.aliasedType = value;
      }
    }
    public virtual void ProvideMembers(){
      if (this.AliasedType == null) return;
      this.Interfaces = new InterfaceList(1);
      if (this.RequireExplicitCoercionFromUnderlyingType)
        this.Interfaces.Add(SystemTypes.TypeDefinition);
      else
        this.Interfaces.Add(SystemTypes.TypeAlias);
      MemberList members = this.members;
      if (members == null) members = this.members = new MemberList();
      //Value field
      Field valueField = new Field(this, null, FieldFlags.Private, StandardIds.Value, this.AliasedType, null);
      members.Add(valueField);
      //Implicit conversion from this type to underlying type
      ParameterList parameters = new ParameterList(1);
      Parameter valuePar = new Parameter(null, ParameterFlags.None, StandardIds.Value, this, null, null);
      parameters.Add(valuePar);
      Method toAliasedType = new Method(this, null, StandardIds.opImplicit, parameters, this.AliasedType, null); 
      toAliasedType.Flags = MethodFlags.Static|MethodFlags.SpecialName|MethodFlags.Public;
      members.Add(toAliasedType);
      StatementList statements = new StatementList(1);
      statements.Add(new Return(new MemberBinding(new UnaryExpression(valuePar, NodeType.AddressOf), valueField)));
      toAliasedType.Body = new Block(statements);
      //Implicit or explicit conversion from underlying type to this type
      Identifier opId =
 this.RequireExplicitCoercionFromUnderlyingType ? StandardIds.opExplicit : StandardIds.opImplicit;
      parameters = new ParameterList(1);
      parameters.Add(valuePar =
 new Parameter(null, ParameterFlags.None, StandardIds.Value, this.AliasedType, null, null));
      Method fromAliasedType = new Method(this, null, opId, parameters, this, null); 
      fromAliasedType.Flags = MethodFlags.Static|MethodFlags.SpecialName|MethodFlags.Public;
      members.Add(fromAliasedType);
      statements = new StatementList(2);
      Local loc = new Local(this);
      statements.Add(new AssignmentStatement(new MemberBinding(new UnaryExpression(loc, NodeType.AddressOf), valueField), valuePar));
      statements.Add(new Return(loc));
      fromAliasedType.Body = new Block(statements);
      this.AddCoercionWrappers(this.AliasedType.ExplicitCoercionMethods, StandardIds.opExplicit, fromAliasedType, toAliasedType);
      this.AddCoercionWrappers(this.AliasedType.ImplicitCoercionMethods, StandardIds.opImplicit, fromAliasedType, toAliasedType);
    }
    private void AddCoercionWrappers(MemberList coercions, Identifier id, Method/*!*/ fromAliasedType, Method/*!*/ toAliasedType) 
      //^ requires this.members != null;
    {
      if (coercions == null) return;
      MemberList members = this.members;
      for (int i = 0, n = coercions.Count; i < n; i++){
        Method coercion = coercions[i] as Method;
        if (coercion == null || coercion.Parameters == null || coercion.Parameters.Count != 1) continue;
        ParameterList parameters = new ParameterList(1);
        Parameter valuePar = new Parameter(null, ParameterFlags.None, StandardIds.Value, null, null, null);
        parameters.Add(valuePar);
        Method m = new Method(this, null, id, parameters, null, null);
        m.Flags = MethodFlags.Static|MethodFlags.SpecialName|MethodFlags.Public;
        Expression arg = valuePar;
        MethodCall call = new MethodCall(new MemberBinding(null, coercion), new ExpressionList(arg));
        if (coercion.ReturnType == this.AliasedType){
          m.ReturnType = this;
          if (this.RequireExplicitCoercionFromUnderlyingType) m.Name = StandardIds.opExplicit;
          valuePar.Type = coercion.Parameters[0].Type;
          call = new MethodCall(new MemberBinding(null, fromAliasedType), new ExpressionList(call));
        }else{
          m.ReturnType = coercion.ReturnType;
          valuePar.Type = this;
          //^ assume call.Operands != null;
          call.Operands[0] = new MethodCall(new MemberBinding(null, toAliasedType), new ExpressionList(arg));
        }
        m.Body = new Block(new StatementList(new Return(call)));
        members.Add(m);
      }
    }
    public override bool IsStructural{
      get{return this.RequireExplicitCoercionFromUnderlyingType;}
    }
    protected TypeNodeList structuralElementTypes;
    public override TypeNodeList StructuralElementTypes{
      get{
        TypeNodeList result = this.structuralElementTypes;
        if (result != null) return result;
        this.structuralElementTypes = result = new TypeNodeList(1);
        result.Add(this.AliasedType);
        return result;
      }
    }
    public override bool IsStructurallyEquivalentTo(TypeNode type){
      if (type == null) return false;
      if (this == type) return true;
      if (this.RequireExplicitCoercionFromUnderlyingType) return false;
      TypeAlias t = type as TypeAlias;
      if (t == null) return false;
      if (t.RequireExplicitCoercionFromUnderlyingType) return false;
      if (this.AliasedType == null || t.AliasedType == null) return false;
      return this.AliasedType == t.AliasedType || this.AliasedType.IsStructurallyEquivalentTo(t.AliasedType);
    }
  }
  public class TypeIntersection : Struct{
    private TypeNodeList types; //sorted by UniqueKey
    public TypeNodeList Types{
      get{
        if (this.types != null) return this.types;
        if (this.ProvideTypeMembers != null) { MemberList mems = this.Members; if (mems != null) mems = null; }
        return this.types;          
      }
      set{
        this.types = value;
      }
    }

    private TypeIntersection(TypeNodeList types, Identifier name) {
      this.NodeType = NodeType.TypeIntersection;
      this.Flags = TypeFlags.Public|TypeFlags.Sealed;
      this.Namespace = StandardIds.StructuralTypes;
      this.Name = name;
      this.Types = types;
      int n = types == null ? 0 : types.Count;
      InterfaceList ifaces = this.Interfaces = new InterfaceList(n+1);
      ifaces.Add(SystemTypes.TypeIntersection);
      if (types != null)
        for (int i = 0; i < n; i++){
          Interface iface = types[i] as Interface;
          if (iface == null) continue;
          ifaces.Add(iface);
        }
      this.isNormalized = true;
    }
    internal TypeIntersection(NestedTypeProvider provideNestedTypes, TypeAttributeProvider provideAttributes, TypeMemberProvider provideMembers, object handle)
      : base(provideNestedTypes, provideAttributes, provideMembers, handle) {
      this.NodeType = NodeType.TypeIntersection;
      this.typeCode = ElementType.ValueType;
    }
    public static TypeIntersection For(TypeNodeList types, Module module) {
      if (module == null) return null;   
      if (types != null && !TypeUnion.AreNormalized(types))   
        types = TypeUnion.Normalize(types);
      TypeFlags visibility = TypeFlags.Public;
      string name = TypeUnion.BuildName(types, "And", ref visibility);
      Identifier tName = Identifier.For(name);
      int tCount = 0;
      TypeIntersection result = null;
      TypeNode t = module.GetStructurallyEquivalentType(StandardIds.StructuralTypes, tName);
      while (t != null){
        //Mangled name is the same. But mangling is not unique, so check for equality.
        TypeIntersection ti = t as TypeIntersection;
        if (ti != null){
          TypeNodeList ts = ti.Types;
          int n = types == null ? 0 : types.Count;
          bool goodMatch = ts != null && ts.Count == n;
          for (int i = 0; goodMatch && i < n; i++) {
            //^ assert types != null && ts != null;
            goodMatch = types[i] == ts[i];
          }
          if (goodMatch) return ti;
        }
        //Mangle some more
        tName = Identifier.For(name+(++tCount).ToString());
        t = module.GetStructurallyEquivalentType(StandardIds.StructuralTypes, tName);
      }
      result = new TypeIntersection(types, tName);
      result.DeclaringModule = module;
      module.Types.Add(result);
      module.StructurallyEquivalentType[tName.UniqueIdKey] = result;
      return result;
    }
    public static TypeIntersection For(TypeNodeList types, TypeNode referringType) {
      if (referringType == null) return null;
      Module module = referringType.DeclaringModule;
      if (module == null) return null;   
      if (types != null && !TypeUnion.AreNormalized(types))   
        types = TypeUnion.Normalize(types);
      TypeFlags visibility = TypeFlags.Public;
      string name = TypeUnion.BuildName(types, "And", ref visibility);
      Identifier tName = Identifier.For(name);
      int tCount = 0;
      TypeIntersection result = null;
      TypeNode t = module.GetStructurallyEquivalentType(StandardIds.StructuralTypes, tName);
      while (t != null){
        //Mangled name is the same. But mangling is not unique, so check for equality.
        TypeIntersection ti = t as TypeIntersection;
        if (ti != null){
          TypeNodeList ts = ti.Types;
          int n = types == null ? 0 : types.Count;
          bool goodMatch = ts != null && ts.Count == n;
          for (int i = 0; goodMatch && i < n; i++) {
            //^ assert ts != null && types != null;
            goodMatch = types[i] == ts[i];
          }
          if (goodMatch) return ti;
        }
        //Mangle some more
        tName = Identifier.For(name+(++tCount).ToString());
        t = module.GetStructurallyEquivalentType(StandardIds.StructuralTypes, tName);
      }
      result = new TypeIntersection(types, tName);
      result.DeclaringModule = module;
      switch (visibility){
        case TypeFlags.NestedFamANDAssem:
        case TypeFlags.NestedFamily:
        case TypeFlags.NestedPrivate:
          referringType.Members.Add(result);
          result.DeclaringType = referringType;
          result.Flags &= ~TypeFlags.VisibilityMask;
          result.Flags |= TypeFlags.NestedPrivate;
          break;
        default:
          module.Types.Add(result);
          break;
      }
      module.StructurallyEquivalentType[tName.UniqueIdKey] = result;
      return result;
    }   
    public override bool IsAssignableTo(TypeNode targetType){
      return targetType == this || targetType == CoreSystemTypes.Object;
    } 
    public override bool IsStructural{
      get{return true;}
    }
    protected TypeNodeList structuralElementTypes;
    public override TypeNodeList StructuralElementTypes{
      get{
        TypeNodeList result = this.structuralElementTypes;
        if (result != null) return result;
        this.structuralElementTypes = result = new TypeNodeList(1);
        TypeNodeList types = this.Types;
        for (int i = 0, n = types == null ? 0 : types.Count; i < n; i++){
          TypeNode t = types[i]; 
          if (t == null) continue;
          result.Add(t);
        }
        return result;
      }
    }
    public override bool IsStructurallyEquivalentTo(TypeNode type){
      if (type == null) return false;
      if (this == type) return true;
      TypeIntersection t = type as TypeIntersection;
      if (t == null) return false;
      return this.IsStructurallyEquivalentList(this.Types, t.Types);
    }
    private TrivialHashtable/*!*/ interfaceMethodFor = new TrivialHashtable();
    public override MemberList Members{
      get{
        MemberList members = this.members;
        if (members == null || this.membersBeingPopulated){
          if (this.ProvideTypeMembers != null){
            lock(this){
              if (this.members != null) return this.members;
              members = base.Members;
              MemberList coercions = this.ExplicitCoercionMethods;
              int n = coercions == null ? 0 : coercions.Count;
              TypeNodeList typeList = this.Types = new TypeNodeList(n);
              for (int i = 0; i < n; i++){
                Method coercion = coercions[i] as Method;
                if (coercion == null) continue;
                typeList.Add(coercion.ReturnType);
              }
            }
            return this.members;
          }
          members = this.Members = new MemberList();
          //Value field
          members.Add(new Field(this, null, FieldFlags.Private, StandardIds.Value, CoreSystemTypes.Object, null));
          //FromObject
          ParameterList parameters = new ParameterList(1);
          parameters.Add(new Parameter(null, ParameterFlags.None, StandardIds.Value, CoreSystemTypes.Object, null, null));
          Method m = new Method(this, null, StandardIds.FromObject, parameters, this, null); 
          m.Flags = MethodFlags.Static|MethodFlags.SpecialName|MethodFlags.Public;
          members.Add(m);
          //coercion operators
          parameters = new ParameterList(1);
          parameters.Add(new Parameter(null, ParameterFlags.None, StandardIds.Value, CoreSystemTypes.Object, null, null));
          m = new Method(this, null, StandardIds.opExplicit, parameters, this, null); 
          m.Flags = MethodFlags.Static|MethodFlags.SpecialName|MethodFlags.Public;
          members.Add(m);
          parameters = new ParameterList(1);
          parameters.Add(new Parameter(null, ParameterFlags.None, StandardIds.Value, this, null, null));
          m = new Method(this, null, StandardIds.opImplicit, parameters, CoreSystemTypes.Object, null); 
          m.Flags = MethodFlags.Static|MethodFlags.SpecialName|MethodFlags.Public;
          members.Add(m);
          TypeNodeList types = this.Types;
          for (int i = 0, n = types.Count; i < n; i++){
            TypeNode t = types[i];
            parameters = new ParameterList(1);
            parameters.Add(new Parameter(null, ParameterFlags.None, StandardIds.Value, this, null, null));
            m = new Method(this, null, StandardIds.opImplicit, parameters, t, null); 
            m.Flags = MethodFlags.Static|MethodFlags.SpecialName|MethodFlags.Public;
            members.Add(m);
          }
          //Routines to forward interface calls to embedded object
          InterfaceList ifaces = this.Interfaces;
          if (ifaces != null){
            for (int i = 0, n = ifaces.Count; i < n; i++){
              Interface iface = ifaces[i];
              if (iface == null) continue;
              MemberList imembers = iface.Members;
              if (imembers == null) continue;
              for (int j = 0, k = imembers.Count; j < k; j++){
                Method imeth = imembers[j] as Method;
                if (imeth == null) continue;
                if (imeth.IsStatic) continue;
                Method meth = (Method)imeth.Clone();
                meth.Flags &= ~MethodFlags.Abstract;
                meth.DeclaringType = this;
                members.Add(meth);
                meth.Parameters = (imeth.Parameters == null ? null : imeth.Parameters.Clone());
                for (int a = 0, b = meth.Parameters == null ? 0 : meth.Parameters.Count; a < b; a++){
                  Parameter par = meth.Parameters[a];
                  if (par == null) continue;
                  meth.Parameters[a] = par = (Parameter)par.Clone();
                  par.DeclaringMethod = meth;
                }
                this.interfaceMethodFor[meth.UniqueKey] = imeth;
              }
            }
          }
          this.ProvideBodiesForMethods();
        }
        return members;
      }
      set{
        this.members = value;
      }
    }
    private void ProvideBodiesForMethods()
      //^ requires this.members != null;
    {
      MemberList members = this.members;
      Field valueField = (Field)members[0];
      //FromObject
      Method fromObject = (Method)members[1];
      StatementList statements = new StatementList(2);
      Local resultLoc = new Local(Identifier.Empty, this);
      Expression param = fromObject.Parameters[0];
      statements.Add(new AssignmentStatement(new MemberBinding(new UnaryExpression(resultLoc, NodeType.AddressOf), valueField), param));
      statements.Add(new Return(resultLoc));
      fromObject.Body = new Block(statements);
      //to coercion
      Method toMethod = (Method)members[2];
      statements = new StatementList(2);
      resultLoc = new Local(Identifier.Empty, this);
      param = toMethod.Parameters[0];
      Expression castExpr = param;
      TypeNodeList types = this.Types;
      int n = types.Count;
      for (int i = 0; i < n; i++){
        TypeNode t = types[i];
        castExpr = new BinaryExpression(castExpr, new Literal(t, CoreSystemTypes.Type), NodeType.Castclass);
      }
      statements.Add(new AssignmentStatement(new MemberBinding(new UnaryExpression(resultLoc, NodeType.AddressOf), valueField), castExpr));
      statements.Add(new Return(resultLoc));
      toMethod.Body = new Block(statements);
      //from coercions
      Method opImplicit = (Method)members[3];
      opImplicit.Body = new Block(statements = new StatementList(1));
      Expression val = new MemberBinding(new UnaryExpression(opImplicit.Parameters[0], NodeType.AddressOf), valueField);
      statements.Add(new Return(val));
      for (int i = 0; i < n; i++){
        TypeNode t = types[i];
        opImplicit = (Method)members[4+i];
        opImplicit.Body = new Block(statements = new StatementList(1));
        val = new MemberBinding(new UnaryExpression(opImplicit.Parameters[0], NodeType.AddressOf), valueField);
        val = new BinaryExpression(val, new Literal(t, CoreSystemTypes.Type), NodeType.Castclass);
        statements.Add(new Return(val));
      }
      //Routines to forward interface calls to embedded object
      for (int i = 4+n, m = members.Count; i < m; i++){
        Method meth = (Method)members[i];
        Method imeth = (Method)this.interfaceMethodFor[meth.UniqueKey];
        Interface iface = (Interface)imeth.DeclaringType;
        statements = new StatementList(2);
        ParameterList parameters = meth.Parameters;
        int k = parameters == null ? 0 : parameters.Count;
        ExpressionList arguments = new ExpressionList(k);
        if (parameters != null)
          for (int j = 0; j < k; j++) arguments.Add(parameters[j]);
        Expression obj =
 new BinaryExpression(new MemberBinding(meth.ThisParameter, valueField), new Literal(iface, CoreSystemTypes.Type), NodeType.Castclass);
        MethodCall mcall = new MethodCall(new MemberBinding(obj, imeth), arguments, NodeType.Callvirt);
        mcall.Type = imeth.ReturnType;
        statements.Add(new ExpressionStatement(mcall));
        statements.Add(new Return());
        meth.Body = new Block(statements);
      }
    }
  }
  public class TypeUnion : Struct{
    private TypeNodeList types; //sorted by UniqueKey
    public TypeNodeList Types{
      get{
        if (this.types != null) return this.types;
        if (this.ProvideTypeMembers != null) { MemberList mems = this.Members; if (mems != null) mems = null; }
        return this.types;          
      }
      set{
        this.types = value;
      }
    }

    private TypeUnion(TypeNodeList types, Identifier tName){
      this.NodeType = NodeType.TypeUnion;
      this.Flags = TypeFlags.Public|TypeFlags.Sealed;
      this.Namespace = StandardIds.StructuralTypes;
      this.Name = tName;
      this.Types = types;
      this.Interfaces = new InterfaceList(SystemTypes.TypeUnion);
      this.isNormalized = true;
    }
    internal TypeUnion(NestedTypeProvider provideNestedTypes, TypeAttributeProvider provideAttributes, TypeMemberProvider provideMembers, object handle)
      : base(provideNestedTypes, provideAttributes, provideMembers, handle) {
      this.NodeType = NodeType.TypeUnion;
      this.typeCode = ElementType.ValueType;
    }

    internal static string/*!*/ BuildName(TypeNodeList types, string separator, ref TypeFlags visibility) {
      int n = types == null ? 0 : types.Count;
      if (n == 0) return "EmtpyUnion";
      StringBuilder sb = new StringBuilder();
      if (types != null)
        for (int i = 0; i < n; i++){
          TypeNode t = types[i];
          if (t == null) continue;
          visibility = TypeNode.GetVisibilityIntersection(visibility, t.Flags & TypeFlags.VisibilityMask);
          sb.Append(t.Name.ToString());
          if (i < n-1) sb.Append(separator);
        }
      return sb.ToString();
    }
    public static bool AreNormalized(TypeNodeList/*!*/ types) {
      int id = 0;
      for (int i = 0, n = types.Count; i < n; i++){
        TypeNode type = types[i];
        if (type == null) continue;
        if (type.UniqueKey <= id || type is TypeUnion) return false;
        id = type.UniqueKey;
      }
      return true;
    }
    public static TypeNodeList/*!*/ Normalize(TypeNodeList/*!*/ types) {
      if (types.Count == 0) return types;
      Hashtable ht = new Hashtable();
      for (int i = 0, n = types.Count; i < n; i++){
        TypeNode type = types[i];
        if (type == null) continue; // error already reported.
        TypeUnion tu = type as TypeUnion;
        if (tu != null){
          for (int ti = 0, tn = tu.Types.Count; ti < tn; ti++){
            type = tu.Types[ti];
            ht[type.UniqueKey] = type;
          }
        }else{
          ht[type.UniqueKey] = type;
        }
      }
      SortedList list = new SortedList(ht);
      TypeNodeList result = new TypeNodeList(list.Count);
      foreach (TypeNode t in list.Values)
        result.Add(t);
      return result;
    }
    public static TypeUnion For(TypeNodeList types, Module module) {
      if (module == null) return null;   
      if (types != null && !TypeUnion.AreNormalized(types))   
        types = TypeUnion.Normalize(types);
      TypeFlags visibility = TypeFlags.Public;
      string name = TypeUnion.BuildName(types, "Or", ref visibility);
      Identifier tName = Identifier.For(name);
      int tCount = 0;
      TypeUnion result = null;
      TypeNode t = module.GetStructurallyEquivalentType(StandardIds.StructuralTypes, tName);
      while (t != null){
        //Mangled name is the same. But mangling is not unique, so check for equality.
        TypeUnion tu = t as TypeUnion;
        if (tu != null){
          TypeNodeList ts = tu.Types;
          int n = types == null ? 0 : types.Count;
          bool goodMatch = ts != null && ts.Count == n;
          for (int i = 0; goodMatch && i < n; i++) {
            //^ assert types != null && ts != null;
            goodMatch = types[i] == ts[i];
          }
          if (goodMatch) return tu;
        }
        //Mangle some more
        tName = Identifier.For(name+(++tCount).ToString());
        t = module.GetStructurallyEquivalentType(StandardIds.StructuralTypes, tName);
      }
      result = new TypeUnion(types, tName);
      result.DeclaringModule = module;
      module.Types.Add(result);
      module.StructurallyEquivalentType[tName.UniqueIdKey] = result;
      return result;
    }
    public static TypeUnion For(TypeNodeList/*!*/ types, TypeNode/*!*/ referringType) {
      Module module = referringType.DeclaringModule;
      if (module == null) return null;
      if (!TypeUnion.AreNormalized(types))   
        types = TypeUnion.Normalize(types);
      TypeFlags visibility = TypeFlags.Public;
      string name = TypeUnion.BuildName(types, "Or", ref visibility);
      Identifier tName = Identifier.For(name);
      int tCount = 0;
      TypeUnion result = null;
      TypeNode t = module.GetStructurallyEquivalentType(StandardIds.StructuralTypes, tName);
      while (t != null){
        //Mangled name is the same. But mangling is not unique, so check for equality.
        TypeUnion tu = t as TypeUnion;
        if (tu != null){
          TypeNodeList ts = tu.Types;
          int n = types.Count;
          bool goodMatch = ts != null && ts.Count == n;
          for (int i = 0; goodMatch && i < n; i++) {
            //^ assert ts != null;
            goodMatch = types[i] == ts[i];
          }
          if (goodMatch) return tu;
        }
        //Mangle some more
        tName = Identifier.For(name+(++tCount).ToString());
        t = module.GetStructurallyEquivalentType(StandardIds.StructuralTypes, tName);
      }
      result = new TypeUnion(types, tName);
      result.DeclaringModule = module;
      switch (visibility){
        case TypeFlags.NestedFamANDAssem:
        case TypeFlags.NestedFamily:
        case TypeFlags.NestedPrivate:
          referringType.Members.Add(result);
          result.DeclaringType = referringType;
          result.Flags &= ~TypeFlags.VisibilityMask;
          result.Flags |= TypeFlags.NestedPrivate;
          break;
        default:
          module.Types.Add(result);
          break;
      }
      module.StructurallyEquivalentType[tName.UniqueIdKey] = result;
      return result;
    }    
    public override bool IsAssignableTo(TypeNode targetType){
      return targetType == this || targetType == CoreSystemTypes.Object;
    }
    public override bool IsStructural{
      get{return true;}
    }
    public override bool IsStructurallyEquivalentTo(TypeNode type){
      if (type == null) return false;
      if (this == type) return true;
      TypeUnion t = type as TypeUnion;
      if (t == null) return false;
      return this.IsStructurallyEquivalentList(this.Types, t.Types);
    }
    protected TypeNodeList structuralElementTypes;
    public override TypeNodeList StructuralElementTypes{
      get{
        TypeNodeList result = this.structuralElementTypes;
        if (result != null) return result;
        this.structuralElementTypes = result = new TypeNodeList(1);
        TypeNodeList types = this.Types;
        for (int i = 0, n = types == null ? 0 : types.Count; i < n; i++){
          TypeNode t = types[i]; 
          if (t == null) continue;
          result.Add(t);
        }
        return result;
      }
    }
    protected TypeUnion unlabeledUnion = null;
    public virtual TypeUnion UnlabeledUnion{
      get{
        TypeUnion result = this.unlabeledUnion;
        if (result != null) return result;
        if (this.Types == null) return this.unlabeledUnion = this;
        TypeNodeList types = this.Types.Clone();
        bool noChange = true;
        for (int i = 0, n = types.Count; i < n; i++){
          TupleType tup = types[i] as TupleType;
          if (tup != null && tup.Members != null && tup.Members.Count == 3 && tup.Members[0] is Field){
            types[i] = ((Field)tup.Members[0]).Type;
            noChange = false;
          }
        }
        if (noChange) 
          return this.unlabeledUnion = this;
        else
          return this.unlabeledUnion = new TypeUnion(types, Identifier.Empty);
      }
    }
    public override MemberList Members{
      get{
        MemberList members = this.members;
        if (members == null || this.membersBeingPopulated){
          if (this.ProvideTypeMembers != null){
            lock(this){
              if (this.members != null) return this.members;
              members = base.Members;
              MemberList coercions = this.ExplicitCoercionMethods;
              int n = coercions == null ? 0 : coercions.Count;
              TypeNodeList typeList = this.Types = new TypeNodeList(n);
              for (int i = 0; i < n; i++){
                Method coercion = coercions[i] as Method;
                if (coercion == null) continue;
                typeList.Add(coercion.ReturnType);
              }
              return this.members;
            }
          }
          members = this.Members = new MemberList();
          //Value field
          members.Add(new Field(this, null, FieldFlags.Private, StandardIds.Value, CoreSystemTypes.Object, null));
          //Tag field
          members.Add(new Field(this, null, FieldFlags.Private, StandardIds.Tag, CoreSystemTypes.UInt32, null));
          //GetValue method (used to convert from subtype to supertype via FromObject on the superType)
          ParameterList parameters = new ParameterList(0);
          Method m = new Method(this, null, StandardIds.GetValue, parameters, CoreSystemTypes.Object, null);
          m.Flags = MethodFlags.SpecialName|MethodFlags.Public; m.CallingConvention = CallingConventionFlags.HasThis;
          members.Add(m);
          //GetTag method (used in typeswitch)
          parameters = new ParameterList(0);
          m = new Method(this, null, StandardIds.GetTag, parameters, CoreSystemTypes.UInt32, null);
          m.Flags = MethodFlags.SpecialName|MethodFlags.Public; m.CallingConvention = CallingConventionFlags.HasThis;
          members.Add(m);
          //GetTagAsType method (used to convert from subtype to supertype via FromObject on the superType)
          parameters = new ParameterList(0);
          m = new Method(this, null, StandardIds.GetTagAsType, parameters, CoreSystemTypes.Type, null);
          m.Flags = MethodFlags.SpecialName|MethodFlags.Public; m.CallingConvention = CallingConventionFlags.HasThis;
          members.Add(m);
          //GetType
          parameters = new ParameterList(0);
          m = new Method(this, null, StandardIds.GetType, parameters, CoreSystemTypes.Type, null);
          m.Flags = MethodFlags.Public; m.CallingConvention = CallingConventionFlags.HasThis;
          members.Add(m);
          //FromObject
          parameters = new ParameterList(2);
          parameters.Add(new Parameter(null, ParameterFlags.None, StandardIds.Value, CoreSystemTypes.Object, null, null));
          parameters.Add(new Parameter(null, ParameterFlags.None, StandardIds.TagType, CoreSystemTypes.Type, null, null));
          m = new Method(this, null, StandardIds.FromObject, parameters, this, null); 
          m.Flags = MethodFlags.Static|MethodFlags.SpecialName|MethodFlags.Public;
          members.Add(m);
          //coercion operators
          TypeNodeList types = this.Types;
          for (int i = 0, n = types.Count; i < n; i++){
            TypeNode t = types[i];
            parameters = new ParameterList(1);
            parameters.Add(new Parameter(null, ParameterFlags.None, StandardIds.Value, t, null, null));
            m = new Method(this, null, StandardIds.opImplicit, parameters, this, null); 
            m.Flags = MethodFlags.Static|MethodFlags.SpecialName|MethodFlags.Public;
            members.Add(m);
            parameters = new ParameterList(1);
            parameters.Add(new Parameter(null, ParameterFlags.None, StandardIds.Value, this, null, null));
            m = new Method(this, null, StandardIds.opExplicit, parameters, t, null); 
            m.Flags = MethodFlags.Static|MethodFlags.SpecialName|MethodFlags.Public;
            members.Add(m);
          }
          this.ProvideBodiesForMethods();
        }
        return members;
      }
      set{
        this.members = value;
      }
    }
    public void ProvideBodiesForMethods(){
      Method objectGetType = CoreSystemTypes.Object.GetMethod(StandardIds.GetType);
      Method typeGetTypeFromHandle =
 (Method)CoreSystemTypes.Type.GetMembersNamed(Identifier.For("GetTypeFromHandle"))[0];
      Method typeGetTypeHandle = (Method)CoreSystemTypes.Type.GetMembersNamed(Identifier.For("get_TypeHandle"))[0];
      Method runtimeTypeHandleGetValue =
 (Method)CoreSystemTypes.RuntimeTypeHandle.GetMembersNamed(Identifier.For("get_Value"))[0];
      if (objectGetType == null || typeGetTypeFromHandle == null || typeGetTypeHandle == null || runtimeTypeHandleGetValue == null) {
        Debug.Fail(""); return;
      }
      MemberList members = this.members;
      if (members == null) return;
      Field valueField = (Field)members[0];
      Field tagField = (Field)members[1];
      //GetValue
      Method getValueMethod = (Method)members[2];
      StatementList statements = new StatementList(1);
      statements.Add(new Return(new MemberBinding(getValueMethod.ThisParameter, valueField)));
      getValueMethod.Body = new Block(statements);
      //GetTag
      Method getTagMethod = (Method)members[3];
      statements = new StatementList(1);
      statements.Add(new Return(new MemberBinding(getTagMethod.ThisParameter, tagField)));
      getTagMethod.Body = new Block(statements);
      //GetTagAsType
      Method getTagAsTypeMethod = (Method)members[4];
      TypeNodeList types = this.Types;
      int n = types.Count;
      Block returnBlock = new Block();
      statements = new StatementList(n+4);
      getTagAsTypeMethod.Body = new Block(statements);
      BlockList targets = new BlockList(n);
      SwitchInstruction sw =
 new SwitchInstruction(new MemberBinding(getTagAsTypeMethod.ThisParameter, tagField), targets);
      statements.Add(sw);
      //TODO: throw an exception
      statements.Add(new ExpressionStatement(new UnaryExpression(new Literal(CoreSystemTypes.Object, CoreSystemTypes.Type), NodeType.Ldtoken))); 
      statements.Add(returnBlock);
      for (int i = 0; i < n; i++){
        TypeNode t = types[i];
        StatementList ldToken = new StatementList(2);
        ldToken.Add(new ExpressionStatement(new UnaryExpression(new Literal(t, CoreSystemTypes.Type), NodeType.Ldtoken)));
        ldToken.Add(new Branch(null, returnBlock));
        Block ldtokBlock = new Block(ldToken);
        targets.Add(ldtokBlock);
        statements.Add(ldtokBlock);
      }
      statements = returnBlock.Statements = new StatementList(1);
      statements.Add(new Return(new MethodCall(new MemberBinding(null, typeGetTypeFromHandle), null)));
      //GetType
      Method getTypeMethod = (Method)members[5];
      statements = new StatementList(4);
      getTypeMethod.Body = new Block(statements);
      MemberBinding mb = new MemberBinding(getTypeMethod.ThisParameter, valueField);
      Local loc = new Local(CoreSystemTypes.Object);
      statements.Add(new AssignmentStatement(loc, mb));
      Block callGetTagAsType = new Block();
      statements.Add(new Branch(new UnaryExpression(loc, NodeType.LogicalNot), callGetTagAsType));
      statements.Add(new Return(new MethodCall(new MemberBinding(loc, objectGetType), null)));
      statements.Add(callGetTagAsType);
      statements.Add(new Return(new MethodCall(new MemberBinding(getTypeMethod.ThisParameter, getTagAsTypeMethod), null)));
      //FromObject
      Method fromObjectMethod = (Method)members[6];
      fromObjectMethod.InitLocals = true;
      statements = new StatementList(n+8); //TODO: get the right expression
      fromObjectMethod.Body = new Block(statements);
      MethodCall getTypeHandle =
 new MethodCall(new MemberBinding(fromObjectMethod.Parameters[1], typeGetTypeHandle), null, NodeType.Callvirt);
      Local handle = new Local(Identifier.Empty, CoreSystemTypes.RuntimeTypeHandle);
      statements.Add(new AssignmentStatement(handle, getTypeHandle));
      MethodCall getValue =
 new MethodCall(new MemberBinding(new UnaryExpression(handle, NodeType.AddressOf), runtimeTypeHandleGetValue), null);
      getValue.Type = CoreSystemTypes.UIntPtr; 
      statements.Add(new ExpressionStatement(getValue));
      Local temp = new Local(Identifier.Empty, CoreSystemTypes.UInt32);
      Local result = new Local(Identifier.Empty, this);
      Expression dup = new Expression(NodeType.Dup);
      Block next = new Block();
      Block curr = new Block();
      Block setTag = new Block();
      for (int i = 0; i < n; i++){
        TypeNode t = types[i];
        StatementList stats = curr.Statements = new StatementList(4);
        UnaryExpression ldtok = new UnaryExpression(new Literal(t, CoreSystemTypes.Type), NodeType.Ldtoken);
        stats.Add(new AssignmentStatement(handle, ldtok));
        getValue =
 new MethodCall(new MemberBinding(new UnaryExpression(handle, NodeType.AddressOf), runtimeTypeHandleGetValue), null);
        Expression compare = new BinaryExpression(dup, getValue, NodeType.Eq);
        stats.Add(new Branch(compare, next));
        stats.Add(new AssignmentStatement(temp, new Literal(i, CoreSystemTypes.UInt32)));
        if (i < n-1)
          stats.Add(new Branch(null, setTag));
        statements.Add(curr);
        curr = next;
        next = new Block();
      }
      statements.Add(curr);
      statements.Add(setTag);
      statements.Add(new ExpressionStatement(new UnaryExpression(null, NodeType.Pop)));
      Expression resultAddr = new UnaryExpression(result, NodeType.AddressOf);
      statements.Add(new AssignmentStatement(new MemberBinding(resultAddr, tagField), temp));
      statements.Add(new AssignmentStatement(new MemberBinding(resultAddr, valueField), fromObjectMethod.Parameters[0]));
      statements.Add(new Return(result));
      for (int i = 0; i < n; i++){
        TypeNode t = types[i];
        if (t == null) continue;
        bool isValueType = t.IsValueType;
        MemberBinding tExpr = new MemberBinding(null, t);
        Method opImplicit = (Method)members[7+i*2];
        opImplicit.Body = new Block(statements = new StatementList(3));
        statements.Add(new AssignmentStatement(new MemberBinding(resultAddr, tagField), new Literal(i, CoreSystemTypes.UInt32)));
        Parameter p0 = opImplicit.Parameters[0];
        p0.Type = t;
        Expression val = p0;
        if (isValueType) val = new BinaryExpression(val, tExpr, NodeType.Box, CoreSystemTypes.Object);
        statements.Add(new AssignmentStatement(new MemberBinding(resultAddr, valueField), val));
        statements.Add(new Return(result));
        Method opExplicit = (Method)members[8+i*2];
        opExplicit.ReturnType = t;
        opExplicit.Body = new Block(statements = new StatementList(1));
        Expression loadValue =
 new MemberBinding(new UnaryExpression(opExplicit.Parameters[0], NodeType.AddressOf), valueField);
        if (isValueType)
          val = new AddressDereference(new BinaryExpression(loadValue, tExpr, NodeType.Unbox), t);
        else
          val = new BinaryExpression(loadValue, tExpr, NodeType.Castclass);
        statements.Add(new Return(val));
      }
    }
  }
  /// <summary>
  /// Bundles a type with a boolean expression. The bundle is a subtype of the given type.
  /// The class is a struct with a single private field of the given type and implicit coercions to and from the underlying type.
  /// The to coercion checks that the constraint is satisfied and throws ArgumentOutOfRangeException if not.
  /// </summary>
  public class ConstrainedType : Struct{
    protected TypeNode underlyingType;
    public TypeNode UnderlyingTypeExpression;
    public Expression Constraint;
    public ConstrainedType(TypeNode/*!*/ underlyingType, Expression/*!*/ constraint) {
      this.NodeType = NodeType.ConstrainedType;
      this.underlyingType = underlyingType;
      this.Flags = TypeFlags.Public|TypeFlags.Sealed|TypeFlags.SpecialName;
      this.Constraint = constraint;
      this.Namespace = StandardIds.StructuralTypes;
      this.Name = Identifier.For("Constrained type:"+base.UniqueKey);
      this.Interfaces = new InterfaceList(SystemTypes.ConstrainedType);
    }
    public ConstrainedType(TypeNode/*!*/ underlyingType, Expression/*!*/ constraint, TypeNode/*!*/ declaringType) {
      this.NodeType = NodeType.ConstrainedType;
      this.underlyingType = underlyingType;
      this.Flags = TypeFlags.NestedPublic|TypeFlags.Sealed|TypeFlags.SpecialName;
      this.Constraint = constraint;
      this.Namespace = StandardIds.StructuralTypes;
      this.Name = Identifier.For("Constrained type:"+base.UniqueKey);
      this.Interfaces = new InterfaceList(SystemTypes.ConstrainedType);
      this.DeclaringType = declaringType;
      this.DeclaringModule = declaringType.DeclaringModule;
      declaringType.Members.Add(this);
    }
    internal ConstrainedType(NestedTypeProvider provideNestedTypes, TypeAttributeProvider provideAttributes, TypeMemberProvider provideMembers, object handle)
      : base(provideNestedTypes, provideAttributes, provideMembers, handle) {
      this.NodeType = NodeType.ConstrainedType;
      this.typeCode = ElementType.ValueType;
    }
    public override bool IsStructural{
      get{return true;}
    }
    protected TypeNodeList structuralElementTypes;
    public override TypeNodeList StructuralElementTypes{
      get{
        TypeNodeList result = this.structuralElementTypes;
        if (result != null) return result;
        this.structuralElementTypes = result = new TypeNodeList(1);
        result.Add(this.UnderlyingType);
        return result;
      }
    }
    public virtual void ProvideMembers(){
      MemberList members = this.members = new MemberList();
      //Value field
      Field valueField = new Field(this, null, FieldFlags.Assembly, StandardIds.Value, this.underlyingType, null);
      members.Add(valueField);
      //Implicit conversion from this type to underlying type
      ParameterList parameters = new ParameterList(1);
      Parameter valuePar = new Parameter(null, ParameterFlags.None, StandardIds.Value, this, null, null);
      parameters.Add(valuePar);
      Method toUnderlying = new Method(this, null, StandardIds.opImplicit, parameters, this.underlyingType, null); 
      toUnderlying.Flags = MethodFlags.Static|MethodFlags.SpecialName|MethodFlags.Public;
      members.Add(toUnderlying);
      //Implicit conversion from underlying type to this type
      parameters = new ParameterList(1);
      parameters.Add(valuePar =
 new Parameter(null, ParameterFlags.None, StandardIds.Value, this.underlyingType, null, null));
      Method fromUnderlying = new Method(this, null, StandardIds.opImplicit, parameters, this, null); 
      fromUnderlying.Flags = MethodFlags.Static|MethodFlags.SpecialName|MethodFlags.Public;
      members.Add(fromUnderlying);
      this.AddCoercionWrappers(this.UnderlyingType.ExplicitCoercionMethods, StandardIds.opExplicit, fromUnderlying, toUnderlying);
      this.AddCoercionWrappers(this.UnderlyingType.ImplicitCoercionMethods, StandardIds.opImplicit, fromUnderlying, toUnderlying);
    }
    private void AddCoercionWrappers(MemberList/*!*/ coercions, Identifier/*!*/ id, Method/*!*/ fromUnderlying, Method/*!*/ toUnderlying) {
      MemberList members = this.members;
      for (int i = 0, n = coercions.Count; i < n; i++){
        Method coercion = coercions[i] as Method;
        if (coercion == null || coercion.Parameters == null || coercion.Parameters.Count != 1) continue;
        ParameterList parameters = new ParameterList(1);
        Parameter valuePar = new Parameter(null, ParameterFlags.None, StandardIds.Value, null, null, null);
        parameters.Add(valuePar);
        Method m = new Method(this, null, id, parameters, null, null);
        m.Flags = MethodFlags.Static|MethodFlags.SpecialName|MethodFlags.Public;
        Expression arg = valuePar;
        MethodCall call = new MethodCall(new MemberBinding(null, coercion), new ExpressionList(arg));
        if (coercion.ReturnType == this.UnderlyingType){
          m.ReturnType = this;
          valuePar.Type = coercion.Parameters[0].Type;
          call = new MethodCall(new MemberBinding(null, fromUnderlying), new ExpressionList(call));
        }else{
          m.ReturnType = coercion.ReturnType;
          valuePar.Type = this;
          call.Operands[0] = new MethodCall(new MemberBinding(null, toUnderlying), new ExpressionList(arg));
        }
        m.Body = new Block(new StatementList(new Return(call)));
        members.Add(m);
      }
    }
    public void ProvideBodiesForMethods(){
      MemberList members = this.members;
      if (members == null) return;
      Field valueField = (Field)members[0];
      //Implicit conversion from this type to underlying type
      Method toUnderlying = (Method)members[1];
      Parameter valuePar = toUnderlying.Parameters[0];
      StatementList statements = new StatementList(1);
      statements.Add(new Return(new MemberBinding(new UnaryExpression(valuePar, NodeType.AddressOf), valueField)));
      toUnderlying.Body = new Block(statements);
      //Implicit conversion from underlying type to this type
      Method fromUnderlying = (Method)members[2];
      valuePar = fromUnderlying.Parameters[0];
      statements = new StatementList(4);
      fromUnderlying.Body = new Block(statements);
      Block succeed = new Block();
      Local temp = new Local(Identifier.Empty, this);
      statements.Add(new Branch(this.Constraint, succeed));
      InstanceInitializer constr = SystemTypes.ArgumentOutOfRangeException.GetConstructor();
      if (constr == null) { Debug.Fail(""); return; }
      MemberBinding argException = new MemberBinding(null, constr);
      statements.Add(new Throw(new Construct(argException, null)));
      statements.Add(succeed);
      statements.Add(new AssignmentStatement(new MemberBinding(new UnaryExpression(temp, NodeType.AddressOf), valueField), valuePar));
      statements.Add(new Return(temp));
    }
    public TypeNode UnderlyingType{
      get{
        TypeNode underlyingType = this.underlyingType;
        if (underlyingType == null){
          Field f = this.GetField(StandardIds.Value);
          if (f != null)
            this.underlyingType = underlyingType = f.Type;
        }
        return underlyingType;
      }
      set{
        this.underlyingType = value;
      }
    }
  }
#endif
    public abstract class TypeModifier : TypeNode
    {
#if !MinimalReader
        public TypeNode ModifierExpression;
        public TypeNode ModifiedTypeExpression;
#endif
        internal TypeModifier(NodeType type, TypeNode /*!*/ modifier, TypeNode /*!*/ modified)
            : base(type)
        {
            Modifier = modifier;
            ModifiedType = modified;
            DeclaringModule = modified.DeclaringModule;
            Namespace = modified.Namespace;
            if (type == NodeType.OptionalModifier)
            {
                typeCode = ElementType.OptionalModifier;
                Name = Identifier.For("optional(" + modifier.Name + ") " + modified.Name);
                fullName = "optional(" + modifier.FullName + ") " + modified.FullName;
            }
            else
            {
                typeCode = ElementType.RequiredModifier;
                Name = Identifier.For("required(" + modifier.Name + ") " + modified.Name);
                fullName = "required(" + modifier.FullName + ") " + modified.FullName;
            }

            Flags = modified.Flags;
        }

        public TypeNode /*!*/ Modifier { get; set; }

        public TypeNode /*!*/ ModifiedType { get; set; }

        public override Node /*!*/ Clone()
        {
            Debug.Assert(false);
            return base.Clone();
        }

        public override string GetFullUnmangledNameWithoutTypeParameters()
        {
            return ModifiedType.GetFullUnmangledNameWithoutTypeParameters();
        }

        public override string GetFullUnmangledNameWithTypeParameters()
        {
            return ModifiedType.GetFullUnmangledNameWithTypeParameters();
        }

        public override string /*!*/ GetUnmangledNameWithoutTypeParameters()
        {
            return ModifiedType.GetUnmangledNameWithoutTypeParameters();
        }

        public override bool IsUnmanaged => ModifiedType.IsUnmanaged;

        public override bool IsStructural => true;

        public override bool IsStructurallyEquivalentTo(TypeNode type, Func<TypeNode, TypeNode> typeSubstitution = null)
        {
            if (type == null) return false;
            if (this == type) return true;
            if (typeSubstitution != null && this == typeSubstitution(type)) return true;
            if (NodeType != type.NodeType) return false;
            var t = type as TypeModifier;
            if (t == null)
            {
                Debug.Assert(false);
                return false;
            }

            if (Modifier != t.Modifier &&
                (Modifier == null || !Modifier.IsStructurallyEquivalentTo(t.Modifier, typeSubstitution)))
                return false;
            if (ModifiedType != t.ModifiedType && (ModifiedType == null ||
                                                   !ModifiedType.IsStructurallyEquivalentTo(t.ModifiedType,
                                                       typeSubstitution)))
                return false;
            return true;
        }

        public override bool IsValueType => ModifiedType.IsValueType;

        public override bool IsPointerType => ModifiedType.IsPointerType;

#if ExtendedRuntime
    public override bool IsPointerFree
    {
      get { return this.ModifiedType.IsPointerFree; }
    }
#endif
        public override bool IsReferenceType => ModifiedType.IsReferenceType;

        public override bool IsTemplateParameter => ModifiedType.IsTemplateParameter;
        protected TypeNodeList structuralElementTypes;
        public override TypeNodeList StructuralElementTypes
        {
            get
            {
                var result = structuralElementTypes;
                if (result != null) return result;
                structuralElementTypes = result = new TypeNodeList(2);
                result.Add(ModifiedType);
                result.Add(Modifier);
                return result;
            }
        }
#if FxCop
    internal override void GetName(TypeFormat options, StringBuilder name)
    {
      if (options.ShowTypeModifiers)
      {
        base.GetName(options, name);
        return;
      }
      this.modifiedType.GetName(options, name);
    }
#endif
    }

    public class OptionalModifier : TypeModifier
    {
        internal OptionalModifier(TypeNode /*!*/ modifier, TypeNode /*!*/ modified)
            : base(NodeType.OptionalModifier, modifier, modified)
        {
        }

        public static OptionalModifier For(TypeNode modifier, TypeNode modified)
        {
            if (modified == null || modifier == null) return null;
            return (OptionalModifier)modified.GetModified(modifier, true);
        }
#if !NoXml
        internal override void AppendDocumentIdMangledName(StringBuilder /*!*/ sb, TypeNodeList methodTypeParameters,
            TypeNodeList typeParameters)
        {
            ModifiedType.AppendDocumentIdMangledName(sb, methodTypeParameters, typeParameters);
            sb.Append('!');
            Modifier.AppendDocumentIdMangledName(sb, methodTypeParameters, typeParameters);
        }
#endif
    }

    public class RequiredModifier : TypeModifier
    {
        internal RequiredModifier(TypeNode /*!*/ modifier, TypeNode /*!*/ modified)
            : base(NodeType.RequiredModifier, modifier, modified)
        {
        }

        public static RequiredModifier /*!*/ For(TypeNode /*!*/ modifier, TypeNode /*!*/ modified)
        {
            return (RequiredModifier)modified.GetModified(modifier, false);
        }
#if !NoXml
        internal override void AppendDocumentIdMangledName(StringBuilder /*!*/ sb, TypeNodeList methodTypeParameters,
            TypeNodeList typeParameters)
        {
            ModifiedType.AppendDocumentIdMangledName(sb, methodTypeParameters, typeParameters);
            sb.Append('|');
            Modifier.AppendDocumentIdMangledName(sb, methodTypeParameters, typeParameters);
        }
#endif
    }
#if !MinimalReader
    public class OptionalModifierTypeExpression : TypeNode, ISymbolicTypeReference
    {
        public TypeNode ModifiedType;
        public TypeNode Modifier;

        public OptionalModifierTypeExpression(TypeNode elementType, TypeNode modifier)
            : base(NodeType.OptionalModifierTypeExpression)
        {
            ModifiedType = elementType;
            Modifier = modifier;
        }

        public OptionalModifierTypeExpression(TypeNode elementType, TypeNode modifier, SourceContext sctx)
            : this(elementType, modifier)
        {
            SourceContext = sctx;
        }

        /// <summary>
        ///     Only needed because IsUnmanaged test is performed in the Looker rather than checker. Once the test
        ///     is moved, this code can be removed.
        /// </summary>
        public override bool IsUnmanaged => ModifiedType == null ? false : ModifiedType.IsUnmanaged;
    }

    public class RequiredModifierTypeExpression : TypeNode, ISymbolicTypeReference
    {
        public TypeNode ModifiedType;
        public TypeNode Modifier;

        public RequiredModifierTypeExpression(TypeNode elementType, TypeNode modifier)
            : base(NodeType.RequiredModifierTypeExpression)
        {
            ModifiedType = elementType;
            Modifier = modifier;
        }

        public RequiredModifierTypeExpression(TypeNode elementType, TypeNode modifier, SourceContext sctx)
            : this(elementType, modifier)
        {
            SourceContext = sctx;
        }

        /// <summary>
        ///     Can be removed once the Unmanaged check moves from Looker to Checker.
        /// </summary>
        public override bool IsUnmanaged => ModifiedType == null ? false : ModifiedType.IsUnmanaged;
    }
#endif
    public class FunctionPointer : TypeNode
    {
        protected TypeNodeList structuralElementTypes;

        internal FunctionPointer(TypeNodeList parameterTypes, TypeNode /*!*/ returnType, Identifier name)
            : base(NodeType.FunctionPointer)
        {
            Name = name;
            Namespace = returnType.Namespace;
            ParameterTypes = parameterTypes;
            ReturnType = returnType;
            typeCode = ElementType.FunctionPointer;
            VarArgStart = int.MaxValue;
        }

        public CallingConventionFlags CallingConvention { get; set; }

        public TypeNodeList ParameterTypes { get; set; }

        public TypeNode ReturnType { get; set; }

        public int VarArgStart { get; set; }

        public override bool IsStatic => (CallingConvention & CallingConventionFlags.HasThis) == 0;

        public override bool IsStructural => true;

        public override TypeNodeList StructuralElementTypes
        {
            get
            {
                var result = structuralElementTypes;
                if (result != null) return result;
                structuralElementTypes = result = new TypeNodeList();
                result.Add(ReturnType);
                var ptypes = ParameterTypes;
                for (int i = 0, n = ptypes == null ? 0 : ptypes.Count; i < n; i++)
                {
                    var ptype = ptypes[i];
                    if (ptype == null) continue;
                    result.Add(ptype);
                }

                return result;
            }
        }

        public override bool IsStructurallyEquivalentTo(TypeNode type, Func<TypeNode, TypeNode> typeSubstitution = null)
        {
            if (type == null) return false;
            if (this == type) return true;
            if (typeSubstitution != null && this == typeSubstitution(type)) return true;
            var t = type as FunctionPointer;
            if (t == null) return false;
            if (Flags != t.Flags || CallingConvention != t.CallingConvention || VarArgStart != t.VarArgStart)
                return false;
            if (ReturnType == null || t.ReturnType == null) return false;
            if (ReturnType != t.ReturnType &&
                !ReturnType.IsStructurallyEquivalentTo(t.ReturnType, typeSubstitution)) return false;
            return IsStructurallyEquivalentList(ParameterTypes, t.ParameterTypes, typeSubstitution);
        }

        public static FunctionPointer /*!*/ For(TypeNodeList /*!*/ parameterTypes, TypeNode /*!*/ returnType)
        {
            var mod = returnType.DeclaringModule;
            if (mod == null)
            {
                Debug.Fail("");
                mod = new Module();
            }

            var sb = new StringBuilder("function pointer ");
            sb.Append(returnType.FullName);
            sb.Append('(');
            for (int i = 0, n = parameterTypes == null ? 0 : parameterTypes.Count; i < n; i++)
            {
                var type = parameterTypes[i];
                if (type == null) continue;
                if (i != 0) sb.Append(',');
                sb.Append(type.FullName);
            }

            sb.Append(')');
            var name = Identifier.For(sb.ToString());
            var t = mod.GetStructurallyEquivalentType(returnType.Namespace, name);
            var counter = 1;
            while (t != null)
            {
                var fp = t as FunctionPointer;
                if (fp != null)
                    if (fp.ReturnType == returnType && ParameterTypesAreEquivalent(fp.ParameterTypes, parameterTypes))
                        return fp;
                name = Identifier.For(name.ToString() + counter++);
                t = mod.GetStructurallyEquivalentType(returnType.Namespace, name);
            }

            var result = t as FunctionPointer;
            if (result == null)
            {
                result = new FunctionPointer(parameterTypes, returnType, name);
                result.DeclaringModule = mod;
                mod.StructurallyEquivalentType[name.UniqueIdKey] = result;
            }

            return result;
        }

        private static bool ParameterTypesAreEquivalent(TypeNodeList list1, TypeNodeList list2)
        {
            if (list1 == null || list2 == null) return list1 == list2;
            var n = list1.Count;
            if (n != list2.Count) return false;
            for (var i = 0; i < n; i++)
                if (list1[i] != list2[i])
                    return false;
            return true;
        }
    }

    public interface ISymbolicTypeReference
    {
    }
#if !MinimalReader
    public class ArrayTypeExpression : ArrayType, ISymbolicTypeReference
    {
        //TODO: add expressions for elementType, rank, sizes and lowerbounds
        public bool LowerBoundIsUnknown;

        public ArrayTypeExpression()
        {
            NodeType = NodeType.ArrayTypeExpression;
        }

        public ArrayTypeExpression(TypeNode /*!*/ elementType, int rank)
            : base(elementType, rank)
        {
            NodeType = NodeType.ArrayTypeExpression;
        }

        public ArrayTypeExpression(TypeNode /*!*/ elementType, int rank, int[] sizes)
            : base(elementType, rank, sizes)
        {
            NodeType = NodeType.ArrayTypeExpression;
        }

        public ArrayTypeExpression(TypeNode /*!*/ elementType, int rank, int[] sizes, int[] lowerBounds)
            : base(elementType, rank, sizes, sizes)
        {
            NodeType = NodeType.ArrayTypeExpression;
        }

        public ArrayTypeExpression(TypeNode /*!*/ elementType, int rank, SourceContext sctx)
            : base(elementType, rank)
        {
            NodeType = NodeType.ArrayTypeExpression;
            SourceContext = sctx;
        }

        public ArrayTypeExpression(TypeNode /*!*/ elementType, int rank, int[] sizes, SourceContext sctx)
            : base(elementType, rank, sizes)
        {
            NodeType = NodeType.ArrayTypeExpression;
            SourceContext = sctx;
        }

        public ArrayTypeExpression(TypeNode /*!*/ elementType, int rank, int[] sizes, int[] lowerBounds,
            SourceContext sctx)
            : base(elementType, rank, sizes, sizes)
        {
            NodeType = NodeType.ArrayTypeExpression;
            SourceContext = sctx;
        }
    }

    public class ClassExpression : Class, ISymbolicTypeReference
    {
        public Expression Expression;

        public ClassExpression(Expression expression)
        {
            NodeType = NodeType.ClassExpression;
            Expression = expression;
        }

        public ClassExpression(Expression expression, TypeNodeList templateArguments)
        {
            NodeType = NodeType.ClassExpression;
            Expression = expression;
            TemplateArguments = templateArguments;
            if (templateArguments != null) TemplateArgumentExpressions = templateArguments.Clone();
        }

        public ClassExpression(Expression expression, SourceContext sctx)
        {
            NodeType = NodeType.ClassExpression;
            Expression = expression;
            SourceContext = sctx;
        }

        public ClassExpression(Expression expression, TypeNodeList templateArguments, SourceContext sctx)
        {
            NodeType = NodeType.ClassExpression;
            Expression = expression;
            TemplateArguments = TemplateArgumentExpressions = templateArguments;
            if (templateArguments != null) TemplateArgumentExpressions = templateArguments.Clone();
            SourceContext = sctx;
        }
    }
#endif
    public class InterfaceExpression : Interface, ISymbolicTypeReference
    {
        public InterfaceExpression(Expression expression)
            : base(null)
        {
            NodeType = NodeType.InterfaceExpression;
            Expression = expression;
        }
#if !MinimalReader
        public InterfaceExpression(Expression expression, SourceContext sctx)
            : base(null)
        {
            NodeType = NodeType.InterfaceExpression;
            Expression = expression;
            SourceContext = sctx;
        }
#endif
        public Expression Expression { get; set; }
    }
#if !MinimalReader
    public class FlexArrayTypeExpression : TypeNode, ISymbolicTypeReference
    {
        public TypeNode ElementType;

        public FlexArrayTypeExpression(TypeNode elementType)
            : base(NodeType.FlexArrayTypeExpression)
        {
            ElementType = elementType;
        }

        public FlexArrayTypeExpression(TypeNode elementType, SourceContext sctx)
            : base(NodeType.FlexArrayTypeExpression)
        {
            ElementType = elementType;
            SourceContext = sctx;
        }
    }

    public class FunctionTypeExpression : TypeNode, ISymbolicTypeReference
    {
        public ParameterList Parameters;
        public TypeNode ReturnType;

        public FunctionTypeExpression(TypeNode returnType, ParameterList parameters)
            : base(NodeType.FunctionTypeExpression)
        {
            ReturnType = returnType;
            Parameters = parameters;
        }

        public FunctionTypeExpression(TypeNode returnType, ParameterList parameters, SourceContext sctx)
            : base(NodeType.FunctionTypeExpression)
        {
            ReturnType = returnType;
            Parameters = parameters;
            SourceContext = sctx;
        }
    }

    public class PointerTypeExpression : Pointer, ISymbolicTypeReference
    {
        public PointerTypeExpression(TypeNode /*!*/ elementType)
            : base(elementType)
        {
            NodeType = NodeType.PointerTypeExpression;
        }

        public PointerTypeExpression(TypeNode /*!*/ elementType, SourceContext sctx)
            : base(elementType)
        {
            NodeType = NodeType.PointerTypeExpression;
            SourceContext = sctx;
        }

        /// <summary>
        ///     This is only needed because the Unmanaged test is done in the Looker rather than the checker.
        ///     (Once the check moves, this can be removed).
        /// </summary>
        public override bool IsUnmanaged => true;
    }

    public class ReferenceTypeExpression : Reference, ISymbolicTypeReference
    {
        public ReferenceTypeExpression(TypeNode /*!*/ elementType)
            : base(elementType)
        {
            NodeType = NodeType.ReferenceTypeExpression;
        }

        public ReferenceTypeExpression(TypeNode /*!*/ elementType, SourceContext sctx)
            : base(elementType)
        {
            NodeType = NodeType.ReferenceTypeExpression;
            SourceContext = sctx;
        }
    }

    public class StreamTypeExpression : TypeNode, ISymbolicTypeReference
    {
        public TypeNode ElementType;

        public StreamTypeExpression(TypeNode elementType)
            : base(NodeType.StreamTypeExpression)
        {
            ElementType = elementType;
        }

        public StreamTypeExpression(TypeNode elementType, SourceContext sctx)
            : base(NodeType.StreamTypeExpression)
        {
            ElementType = elementType;
            SourceContext = sctx;
        }
    }

    public class NonEmptyStreamTypeExpression : TypeNode, ISymbolicTypeReference
    {
        public TypeNode ElementType;

        public NonEmptyStreamTypeExpression(TypeNode elementType)
            : base(NodeType.NonEmptyStreamTypeExpression)
        {
            ElementType = elementType;
        }

        public NonEmptyStreamTypeExpression(TypeNode elementType, SourceContext sctx)
            : base(NodeType.NonEmptyStreamTypeExpression)
        {
            ElementType = elementType;
            SourceContext = sctx;
        }
    }

    public class BoxedTypeExpression : TypeNode, ISymbolicTypeReference
    {
        public TypeNode ElementType;

        public BoxedTypeExpression(TypeNode elementType)
            : base(NodeType.BoxedTypeExpression)
        {
            ElementType = elementType;
        }

        public BoxedTypeExpression(TypeNode elementType, SourceContext sctx)
            : base(NodeType.BoxedTypeExpression)
        {
            ElementType = elementType;
            SourceContext = sctx;
        }
    }

    public class InvariantTypeExpression : TypeNode, ISymbolicTypeReference
    {
        public TypeNode ElementType;

        public InvariantTypeExpression(TypeNode elementType)
            : base(NodeType.InvariantTypeExpression)
        {
            ElementType = elementType;
        }

        public InvariantTypeExpression(TypeNode elementType, SourceContext sctx)
            : base(NodeType.InvariantTypeExpression)
        {
            ElementType = elementType;
            SourceContext = sctx;
        }
    }

    public class NonNullTypeExpression : TypeNode, ISymbolicTypeReference
    {
        public TypeNode ElementType;

        public NonNullTypeExpression(TypeNode elementType)
            : base(NodeType.NonNullTypeExpression)
        {
            ElementType = elementType;
        }

        public NonNullTypeExpression(TypeNode elementType, SourceContext sctx)
            : base(NodeType.NonNullTypeExpression)
        {
            ElementType = elementType;
            SourceContext = sctx;
        }
    }

    public class NonNullableTypeExpression : TypeNode, ISymbolicTypeReference
    {
        public TypeNode ElementType;

        public NonNullableTypeExpression(TypeNode elementType)
            : base(NodeType.NonNullableTypeExpression)
        {
            ElementType = elementType;
        }

        public NonNullableTypeExpression(TypeNode elementType, SourceContext sctx)
            : base(NodeType.NonNullableTypeExpression)
        {
            ElementType = elementType;
            SourceContext = sctx;
        }
    }

    public class NullableTypeExpression : TypeNode, ISymbolicTypeReference
    {
        public TypeNode ElementType;

        public NullableTypeExpression(TypeNode elementType)
            : base(NodeType.NullableTypeExpression)
        {
            ElementType = elementType;
        }

        public NullableTypeExpression(TypeNode elementType, SourceContext sctx)
            : base(NodeType.NullableTypeExpression)
        {
            ElementType = elementType;
            SourceContext = sctx;
        }
    }

    public class TupleTypeExpression : TypeNode, ISymbolicTypeReference
    {
        public FieldList Domains;

        public TupleTypeExpression(FieldList domains)
            : base(NodeType.TupleTypeExpression)
        {
            Domains = domains;
        }

        public TupleTypeExpression(FieldList domains, SourceContext sctx)
            : base(NodeType.TupleTypeExpression)
        {
            Domains = domains;
            SourceContext = sctx;
        }
    }

    public class TypeIntersectionExpression : TypeNode, ISymbolicTypeReference
    {
        public TypeNodeList Types;

        public TypeIntersectionExpression(TypeNodeList types)
            : base(NodeType.TypeIntersectionExpression)
        {
            Types = types;
        }

        public TypeIntersectionExpression(TypeNodeList types, SourceContext sctx)
            : base(NodeType.TypeIntersectionExpression)
        {
            Types = types;
            SourceContext = sctx;
        }
    }

    public class TypeUnionExpression : TypeNode, ISymbolicTypeReference
    {
        public TypeNodeList Types;

        public TypeUnionExpression(TypeNodeList types)
            : base(NodeType.TypeUnionExpression)
        {
            Types = types;
        }

        public TypeUnionExpression(TypeNodeList types, SourceContext sctx)
            : base(NodeType.TypeUnionExpression)
        {
            Types = types;
            SourceContext = sctx;
        }
    }

    public class TypeExpression : TypeNode, ISymbolicTypeReference
    {
        public int Arity;
        public Expression Expression;

        public TypeExpression(Expression expression)
            : base(NodeType.TypeExpression)
        {
            Expression = expression;
        }

        public TypeExpression(Expression expression, TypeNodeList templateArguments)
            : base(NodeType.TypeExpression)
        {
            Expression = expression;
            this.templateArguments = TemplateArgumentExpressions = templateArguments;
        }

        public TypeExpression(Expression expression, int arity)
            : base(NodeType.TypeExpression)
        {
            Expression = expression;
            Arity = arity;
        }

        public TypeExpression(Expression expression, SourceContext sctx)
            : base(NodeType.TypeExpression)
        {
            Expression = expression;
            SourceContext = sctx;
        }

        public TypeExpression(Expression expression, TypeNodeList templateArguments, SourceContext sctx)
            : base(NodeType.TypeExpression)
        {
            Expression = expression;
            this.templateArguments = TemplateArgumentExpressions = templateArguments;
            SourceContext = sctx;
        }

        public TypeExpression(Expression expression, int arity, SourceContext sctx)
            : base(NodeType.TypeExpression)
        {
            Expression = expression;
            Arity = arity;
            SourceContext = sctx;
        }

        public override bool IsUnmanaged
        {
            get
            {
                var lit = Expression as Literal;
                if (lit != null)
                {
                    var t = lit.Value as TypeNode;
                    if (t != null) return t.IsUnmanaged;
                    if (lit.Value is TypeCode) return true;
                }

                return true;
            }
        }
    }

    public class TypeReference : Node
    {
        public TypeNode Expression;
        public TypeNode Type;

        public TypeReference(TypeNode typeExpression)
            : base(NodeType.TypeReference)
        {
            Expression = typeExpression;
            if (typeExpression != null)
                SourceContext = typeExpression.SourceContext;
        }

        public TypeReference(TypeNode typeExpression, TypeNode type)
            : base(NodeType.TypeReference)
        {
            Expression = typeExpression;
            Type = type;
            if (typeExpression != null)
                SourceContext = typeExpression.SourceContext;
        }

        public static explicit operator TypeNode(TypeReference typeReference)
        {
            return null == (object)typeReference ? null : typeReference.Type;
        }

        public static bool operator ==(TypeReference typeReference, TypeNode type)
        {
            return null == (object)typeReference ? null == (object)type : typeReference.Type == type;
        }

        public static bool operator ==(TypeNode type, TypeReference typeReference)
        {
            return null == (object)typeReference ? null == (object)type : typeReference.Type == type;
        }

        public static bool operator !=(TypeReference typeReference, TypeNode type)
        {
            return null == (object)typeReference ? null != (object)type : typeReference.Type != type;
        }

        public static bool operator !=(TypeNode type, TypeReference typeReference)
        {
            return null == (object)typeReference ? null != (object)type : typeReference.Type != type;
        }

        public override bool Equals(object obj)
        {
            return obj == this || obj == Type;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    public class ArglistArgumentExpression : NaryExpression
    {
        public ArglistArgumentExpression(ExpressionList args, SourceContext sctx) : base(args,
            NodeType.ArglistArgumentExpression)
        {
            SourceContext = sctx;
        }
    }

    public class ArglistExpression : Expression
    {
        public ArglistExpression(SourceContext sctx) : base(NodeType.ArglistExpression)
        {
            SourceContext = sctx;
        }
    }

    public class RefValueExpression : BinaryExpression
    {
        public RefValueExpression(Expression typedreference, Expression type, SourceContext sctx)
            : base(typedreference, type, NodeType.RefValueExpression)
        {
            SourceContext = sctx;
        }
    }

    public class RefTypeExpression : UnaryExpression
    {
        public RefTypeExpression(Expression typedreference, SourceContext sctx)
            : base(typedreference, NodeType.RefTypeExpression)
        {
            SourceContext = sctx;
        }
    }
#endif
#if ExtendedRuntime
  public delegate Expression Coercer(Expression source, TypeNode targetType, TypeViewer typeViewer);
  public sealed class StreamAdapter{
    private StreamAdapter(){}
    public static TypeNode For(Interface/*!*/ sourceStream, Interface/*!*/ targetStream, Module/*!*/ module, Coercer/*!*/ coercer, SourceContext sctx) {
      return StreamAdapter.For(sourceStream, targetStream, null, module, coercer, sctx);
    }
    public static TypeNode For(Interface /*!*/sourceStream, Interface/*!*/ targetStream, TypeNode/*!*/ referringType, Coercer/*!*/ coercer, SourceContext sctx) {
      if (referringType == null){Debug.Assert(false); return null;}
      return StreamAdapter.For(sourceStream, targetStream, referringType, referringType.DeclaringModule, coercer, sctx);
    }
    public static TypeNode For(Interface/*!*/ sourceStream, Interface/*!*/ targetStream, TypeNode referringType, Module/*!*/ module, Coercer/*!*/ coercer, SourceContext sctx) {
      Debug.Assert(sourceStream.Template == SystemTypes.GenericIEnumerable && targetStream.Template == SystemTypes.GenericIEnumerable);
      Identifier id = Identifier.For("AdapterFor" + sourceStream.Name + "To" + targetStream.Name);
      for (int i = 1; ;i++){
        TypeNode t = module.GetStructurallyEquivalentType(targetStream.Namespace, id);
        if (t == null) break;
        if (t.IsAssignableTo(targetStream)){
          InstanceInitializer cons = t.GetConstructor(sourceStream);
          if (cons != null) return t;
        }
        id = Identifier.For(id.ToString()+i);
      }
      Method sGetEnumerator = sourceStream.GetMethod(StandardIds.GetEnumerator);
      Method tGetEnumerator = targetStream.GetMethod(StandardIds.GetEnumerator);
      if (sGetEnumerator == null || tGetEnumerator == null) { Debug.Fail(""); return null; }
      Interface sGetEnumeratorReturnType = (Interface)TypeNode.StripModifiers(sGetEnumerator.ReturnType);
      Interface tGetEnumeratorReturnType = (Interface)TypeNode.StripModifiers(tGetEnumerator.ReturnType);
      //^ assert sGetEnumeratorReturnType != null && tGetEnumeratorReturnType != null;
      TypeNode enumeratorAdapter = null;
      if (referringType != null)
        enumeratorAdapter =
 EnumeratorAdapter.For(id, sGetEnumeratorReturnType, tGetEnumeratorReturnType, referringType, coercer, sctx);
      else
        enumeratorAdapter =
 EnumeratorAdapter.For(id, sGetEnumeratorReturnType, tGetEnumeratorReturnType, module, coercer, sctx);
      if (enumeratorAdapter == null) return null;
      InterfaceList interfaces = new InterfaceList(targetStream);
      MemberList members = new MemberList(3);
      Class adapter =
 new Class(module, null, null, TypeFlags.Sealed, targetStream.Namespace, id, CoreSystemTypes.Object, interfaces, members);
      adapter.IsNormalized = true;
      if (referringType == null ||
      (sourceStream.Flags & TypeFlags.VisibilityMask) == TypeFlags.Public && (targetStream.Flags & TypeFlags.VisibilityMask) == TypeFlags.Public){
        adapter.Flags |= TypeFlags.Public;
        module.Types.Add(adapter);
      }else{
        adapter.Flags |= TypeFlags.NestedPrivate;
        referringType.Members.Add(adapter);
        adapter.DeclaringType = referringType;
      }
      module.StructurallyEquivalentType[id.UniqueIdKey] = adapter;

      //Field to hold source stream
      Field ssField = new Field(adapter, null, FieldFlags.Private, StandardIds.Value, sourceStream, null);
      members.Add(ssField);

      //Constructor
      This ThisParameter = new This(adapter);
      Parameter par = new Parameter(null, ParameterFlags.None, StandardIds.Value, sourceStream, null, null);
      StatementList statements = new StatementList(3);
      InstanceInitializer cstr = CoreSystemTypes.Object.GetConstructor();
      if (cstr == null) { Debug.Fail(""); return adapter; }
      MethodCall mcall =
 new MethodCall(new MemberBinding(ThisParameter, cstr), new ExpressionList(0), NodeType.Call, CoreSystemTypes.Void);
      statements.Add(new ExpressionStatement(mcall));
      statements.Add(new AssignmentStatement(new MemberBinding(ThisParameter, ssField), par));
      statements.Add(new Return());
      InstanceInitializer acons = new InstanceInitializer(adapter, null, new ParameterList(par), new Block(statements));
      acons.Flags |= MethodFlags.Public;
      acons.ThisParameter = ThisParameter;
      members.Add(acons);

      //GetEnumerator
      ThisParameter = new This(adapter);
      statements = new StatementList(1);
      mcall = new MethodCall(new MemberBinding(new MemberBinding(ThisParameter, ssField), sGetEnumerator),
        new ExpressionList(0), NodeType.Callvirt, sGetEnumerator.ReturnType);
      cstr = enumeratorAdapter.GetConstructor(sGetEnumerator.ReturnType);
      if (cstr == null) { Debug.Fail(""); return adapter; }
      Construct constr = new Construct(new MemberBinding(null, cstr), new ExpressionList(mcall));
      statements.Add(new Return(constr));
      Method getEnumerator =
 new Method(adapter, null, StandardIds.GetEnumerator, null, tGetEnumerator.ReturnType, new Block(statements));
      getEnumerator.Flags = MethodFlags.Public | MethodFlags.Virtual | MethodFlags.NewSlot | MethodFlags.HideBySig;
      getEnumerator.CallingConvention = CallingConventionFlags.HasThis;
      getEnumerator.ThisParameter = ThisParameter;
      members.Add(getEnumerator);

      //IEnumerable.GetEnumerator
      Method ieGetEnumerator = SystemTypes.IEnumerable.GetMethod(StandardIds.GetEnumerator);
      if (ieGetEnumerator == null) { Debug.Fail(""); return adapter; }
      ThisParameter = new This(adapter);
      statements = new StatementList(1);
      mcall = new MethodCall(new MemberBinding(new MemberBinding(ThisParameter, ssField), ieGetEnumerator),
        new ExpressionList(0), NodeType.Callvirt, SystemTypes.IEnumerator);
      statements.Add(new Return(mcall));
      getEnumerator =
 new Method(adapter, null, StandardIds.IEnumerableGetEnumerator, null, SystemTypes.IEnumerator, new Block(statements));
      getEnumerator.ThisParameter = ThisParameter;
      getEnumerator.ImplementedInterfaceMethods = new MethodList(ieGetEnumerator);
      getEnumerator.CallingConvention = CallingConventionFlags.HasThis;
      getEnumerator.Flags = MethodFlags.Private | MethodFlags.Virtual | MethodFlags.SpecialName;
      members.Add(getEnumerator);

      return adapter;
    }
  }
  internal sealed class EnumeratorAdapter{
    private EnumeratorAdapter(){}
    internal static TypeNode For(Identifier/*!*/ id, Interface/*!*/ sourceIEnumerator, Interface/*!*/ targetIEnumerator, Module/*!*/ module, Coercer/*!*/ coercer, SourceContext sctx) {
      return EnumeratorAdapter.For(id, sourceIEnumerator, targetIEnumerator, null, module, coercer, sctx);
    }
    internal static TypeNode For(Identifier/*!*/ id, Interface/*!*/ sourceIEnumerator, Interface/*!*/ targetIEnumerator, TypeNode/*!*/ referringType, Coercer/*!*/ coercer, SourceContext sctx) {
      if (referringType == null){Debug.Assert(false); return null;}
      return EnumeratorAdapter.For(id, sourceIEnumerator, targetIEnumerator, referringType, referringType.DeclaringModule, coercer, sctx);
    }
    private static TypeNode For(Identifier/*!*/ id, Interface/*!*/ sourceIEnumerator, Interface/*!*/ targetIEnumerator, TypeNode referringType, Module/*!*/ module, Coercer/*!*/ coercer, SourceContext sctx) {
      Method sGetCurrent = sourceIEnumerator.GetMethod(StandardIds.getCurrent);
      if (sGetCurrent == null) { Debug.Fail(""); return null; }
      Method sMoveNext = sourceIEnumerator.GetMethod(StandardIds.MoveNext);
      if (sMoveNext == null) sMoveNext = SystemTypes.IEnumerator.GetMethod(StandardIds.MoveNext);
      Method tGetCurrent = targetIEnumerator.GetMethod(StandardIds.getCurrent);
      if (tGetCurrent == null) { Debug.Fail(""); return null; }
      Method tMoveNext = targetIEnumerator.GetMethod(StandardIds.MoveNext);
      if (tMoveNext == null) tMoveNext = SystemTypes.IEnumerator.GetMethod(StandardIds.MoveNext);
      Local loc = new Local(sGetCurrent.ReturnType);
      Expression curr = coercer(loc, tGetCurrent.ReturnType, null);
      if (curr == null) return null;
      id = Identifier.For("Enumerator"+id.ToString());
      InterfaceList interfaces = new InterfaceList(targetIEnumerator, SystemTypes.IDisposable);
      MemberList members = new MemberList(5);
      Class adapter =
 new Class(module, null, null, TypeFlags.Public, targetIEnumerator.Namespace, id, CoreSystemTypes.Object, interfaces, members);
      adapter.IsNormalized = true;
      if (referringType == null ||
      (sourceIEnumerator.Flags & TypeFlags.VisibilityMask) == TypeFlags.Public && (targetIEnumerator.Flags & TypeFlags.VisibilityMask) == TypeFlags.Public){
        adapter.Flags |= TypeFlags.Public;
        module.Types.Add(adapter);
      }else{
        adapter.Flags |= TypeFlags.NestedPrivate;
        referringType.Members.Add(adapter);
        adapter.DeclaringType = referringType;
      }
      //Field to hold source enumerator
      Field seField = new Field(adapter, null, FieldFlags.Private, StandardIds.Value, sourceIEnumerator, null);
      members.Add(seField);

      //Constructor
      Parameter par = new Parameter(null, ParameterFlags.None, StandardIds.Value, sourceIEnumerator, null, null);
      StatementList statements = new StatementList(3);
      This ThisParameter = new This(adapter);
      InstanceInitializer constr = CoreSystemTypes.Object.GetConstructor();
      if (constr == null) { Debug.Fail(""); return null; }
      MethodCall mcall = new MethodCall(new MemberBinding(ThisParameter, constr),
        new ExpressionList(0), NodeType.Call, CoreSystemTypes.Void);
      statements.Add(new ExpressionStatement(mcall));
      statements.Add(new AssignmentStatement(new MemberBinding(ThisParameter, seField), par));
      statements.Add(new Return());
      InstanceInitializer acons = new InstanceInitializer(adapter, null, new ParameterList(par), new Block(statements));
      acons.Flags |= MethodFlags.Public;
      acons.ThisParameter = ThisParameter;
      members.Add(acons);

      //get_Current
      statements = new StatementList(2);
      ThisParameter = new This(adapter);
      mcall = new MethodCall(new MemberBinding(new MemberBinding(ThisParameter, seField), sGetCurrent),
        new ExpressionList(0), NodeType.Callvirt, sGetCurrent.ReturnType);
      mcall.SourceContext = sctx;
      statements.Add(new AssignmentStatement(loc, mcall));
      statements.Add(new Return(curr));
      Method getCurrent =
 new Method(adapter, null, StandardIds.getCurrent, null, tGetCurrent.ReturnType, new Block(statements));
      getCurrent.Flags =
 MethodFlags.Public | MethodFlags.Virtual | MethodFlags.NewSlot | MethodFlags.HideBySig | MethodFlags.SpecialName;
      getCurrent.CallingConvention = CallingConventionFlags.HasThis;
      getCurrent.ThisParameter = ThisParameter;
      members.Add(getCurrent);

      //IEnumerator.GetCurrent
      statements = new StatementList(1);
      ThisParameter = new This(adapter);
      MethodCall callGetCurrent =
 new MethodCall(new MemberBinding(ThisParameter, getCurrent), new ExpressionList(0), NodeType.Call, getCurrent.ReturnType);
      if (getCurrent.ReturnType.IsValueType) {
        MemberBinding etExpr = new MemberBinding(null, getCurrent.ReturnType);
        statements.Add(new Return(new BinaryExpression(callGetCurrent, etExpr, NodeType.Box, CoreSystemTypes.Object)));
      }else
        statements.Add(new Return(callGetCurrent));
      Method ieGetCurrent =
 new Method(adapter, null, StandardIds.IEnumeratorGetCurrent, null, CoreSystemTypes.Object, new Block(statements));
      ieGetCurrent.ThisParameter = ThisParameter;
      ieGetCurrent.ImplementedInterfaceMethods =
 new MethodList(SystemTypes.IEnumerator.GetMethod(StandardIds.getCurrent));
      ieGetCurrent.CallingConvention = CallingConventionFlags.HasThis;
      ieGetCurrent.Flags = MethodFlags.Private | MethodFlags.Virtual | MethodFlags.SpecialName;
      members.Add(ieGetCurrent);

      //IEnumerator.Reset
      Method ieReset = SystemTypes.IEnumerator.GetMethod(StandardIds.Reset);
      if (ieReset == null) { Debug.Fail(""); return null; }
      statements = new StatementList(2);
      ThisParameter = new This(adapter);
      MethodCall callSourceReset =
 new MethodCall(new MemberBinding(ThisParameter, ieReset), new ExpressionList(0), NodeType.Callvirt, CoreSystemTypes.Object);
      statements.Add(new ExpressionStatement(callSourceReset));
      statements.Add(new Return());
      Method reset =
 new Method(adapter, null, StandardIds.IEnumeratorReset, null, CoreSystemTypes.Void, new Block(statements));
      reset.ThisParameter = ThisParameter;
      reset.ImplementedInterfaceMethods = new MethodList(ieReset);
      reset.CallingConvention = CallingConventionFlags.HasThis;
      reset.Flags = MethodFlags.Private | MethodFlags.Virtual | MethodFlags.SpecialName;
      members.Add(reset);

      //MoveNext
      if (sMoveNext == null) { Debug.Fail(""); return null; }
      statements = new StatementList(1);
      ThisParameter = new This(adapter);
      mcall = new MethodCall(new MemberBinding(new MemberBinding(ThisParameter, seField), sMoveNext),
        new ExpressionList(0), NodeType.Callvirt, CoreSystemTypes.Boolean);
      statements.Add(new Return(mcall));
      Method moveNext =
 new Method(adapter, null, StandardIds.MoveNext, null, CoreSystemTypes.Boolean, new Block(statements));
      moveNext.Flags = MethodFlags.Public | MethodFlags.Virtual | MethodFlags.NewSlot | MethodFlags.HideBySig;
      moveNext.CallingConvention = CallingConventionFlags.HasThis;
      moveNext.ThisParameter = ThisParameter;
      members.Add(moveNext);

      //IDispose.Dispose
      statements = new StatementList(1);
      //TODO: call Dispose on source enumerator
      statements.Add(new Return());
      Method dispose =
 new Method(adapter, null, StandardIds.Dispose, null, CoreSystemTypes.Void, new Block(statements));
      dispose.CallingConvention = CallingConventionFlags.HasThis;
      dispose.Flags = MethodFlags.Public | MethodFlags.Virtual;
      adapter.Members.Add(dispose);
      return adapter;
    }
  }
#endif
#if FxCop
  public class EventNode : Member{
#else
    public class Event : Member
    {
#endif
#if !MinimalReader
        public TypeNode HandlerTypeExpression;

        /// <summary>
        ///     The list of types (just one in C#) that contain abstract or virtual events that are explicity implemented or
        ///     overridden by this event.
        /// </summary>
        public TypeNodeList ImplementedTypes;

        public TypeNodeList ImplementedTypeExpressions;

        /// <summary>Provides a delegate instance that is added to the event upon initialization.</summary>
        public Expression InitialHandler;

        public Field BackingField;
#endif
#if FxCop
    public EventNode()
#else
        public Event()
#endif
            : base(NodeType.Event)
        {
        }
#if !MinimalReader
        public Event(TypeNode declaringType, AttributeList attributes, EventFlags flags, Identifier name,
            Method handlerAdder, Method handlerCaller, Method handlerRemover, TypeNode handlerType)
            : base(declaringType, attributes, name, NodeType.Event)
        {
            Flags = flags;
            HandlerAdder = handlerAdder;
            HandlerCaller = handlerCaller;
            HandlerRemover = handlerRemover;
            HandlerType = handlerType;
        }
#endif
        /// <summary>Bits characterizing this event.</summary>
        public EventFlags Flags { get; set; }

        /// <summary>
        ///     The method to be called in order to add a handler to an event. Corresponds to the add clause of a C# event
        ///     declaration.
        /// </summary>
        public Method HandlerAdder { get; set; }

        /// <summary>The method that gets called to fire an event. There is no corresponding C# syntax.</summary>
        public Method HandlerCaller { get; set; }

        public MethodFlags HandlerFlags { get; set; }

        /// <summary>
        ///     The method to be called in order to remove a handler from an event. Corresponds to the remove clause of a C#
        ///     event declaration.
        /// </summary>
        public Method HandlerRemover { get; set; }

        /// <summary>
        ///     The delegate type that a handler for this event must have. Corresponds to the type clause of C# event
        ///     declaration.
        /// </summary>
        public TypeNode HandlerType { get; set; }

        public MethodList OtherMethods { get; set; }

        protected string fullName;
        public override string /*!*/ FullName
        {
            get
            {
                var result = fullName;
                if (result == null)
                    fullName = result = DeclaringType.FullName + "." + (Name == null ? "" : Name.ToString());
                return result;
            }
        }
#if !NoXml
        protected override Identifier GetDocumentationId()
        {
            var sb = new StringBuilder();
            sb.Append("E:");
            if (DeclaringType == null) return Identifier.Empty;
            DeclaringType.AppendDocumentIdMangledName(sb, null, null);
            sb.Append(".");
            if (Name == null) return Identifier.Empty;
            sb.Append(Name.Name);
            return Identifier.For(sb.ToString());
        }
#endif
#if !NoReflection
        public static Event GetEvent(EventInfo eventInfo)
        {
            if (eventInfo == null) return null;
            var tn = TypeNode.GetTypeNode(eventInfo.DeclaringType);
            if (tn == null) return null;
            return tn.GetEvent(Identifier.For(eventInfo.Name));
        }

        protected EventInfo eventInfo;
        public virtual EventInfo GetEventInfo()
        {
            if (eventInfo == null)
            {
                var tn = DeclaringType;
                if (tn == null) return null;
                var t = tn.GetRuntimeType();
                if (t == null) return null;
                var flags = BindingFlags.DeclaredOnly;
                if (IsPublic) flags |= BindingFlags.Public;
                else flags |= BindingFlags.NonPublic;
                if (IsStatic) flags |= BindingFlags.Static;
                else flags |= BindingFlags.Instance;
                eventInfo = t.GetEvent(Name.ToString(), flags);
            }

            return eventInfo;
        }
#endif
        /// <summary>
        ///     True if the methods constituting this event are abstract.
        /// </summary>
        public bool IsAbstract => (HandlerFlags & MethodFlags.Abstract) != 0;

        public override bool IsAssembly => (HandlerFlags & MethodFlags.MethodAccessMask) == MethodFlags.Assembly;

        public override bool IsCompilerControlled =>
            (HandlerFlags & MethodFlags.MethodAccessMask) == MethodFlags.CompilerControlled;

        public override bool IsFamily => (HandlerFlags & MethodFlags.MethodAccessMask) == MethodFlags.Family;

        public override bool IsFamilyAndAssembly =>
            (HandlerFlags & MethodFlags.MethodAccessMask) == MethodFlags.FamANDAssem;

        public override bool IsFamilyOrAssembly =>
            (HandlerFlags & MethodFlags.MethodAccessMask) == MethodFlags.FamORAssem;

        public bool IsFinal => (HandlerFlags & MethodFlags.Final) != 0;

        public override bool IsPrivate => (HandlerFlags & MethodFlags.MethodAccessMask) == MethodFlags.Private;

        public override bool IsPublic => (HandlerFlags & MethodFlags.MethodAccessMask) == MethodFlags.Public;
        public override bool IsSpecialName => (Flags & EventFlags.SpecialName) != 0;

        public override bool IsStatic => (HandlerFlags & MethodFlags.Static) != 0;

        /// <summary>
        ///     True if that the methods constituting this event are virtual.
        /// </summary>
        public bool IsVirtual => (HandlerFlags & MethodFlags.Virtual) != 0;

        public override bool IsVisibleOutsideAssembly =>
            (HandlerAdder != null && HandlerAdder.IsVisibleOutsideAssembly) ||
            (HandlerCaller != null && HandlerCaller.IsVisibleOutsideAssembly) ||
            (HandlerRemover != null && HandlerRemover.IsVisibleOutsideAssembly);

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Event NotSpecified = new Event();

        public override Member HiddenMember
        {
            get { return HiddenEvent; }
            set { HiddenEvent = (Event)value; }
        }

        protected Property hiddenEvent;

        public virtual Event HiddenEvent
        {
            get
            {
                if (hiddenMember == NotSpecified) return null;
                var hiddenEvent = hiddenMember as Event;
                if (hiddenEvent != null) return hiddenEvent;

                var hiddenAdder = HandlerAdder == null ? null : HandlerAdder.HiddenMethod;
                var hiddenCaller = HandlerCaller == null ? null : HandlerCaller.HiddenMethod;
                var hiddenRemover = HandlerRemover == null ? null : HandlerRemover.HiddenMethod;
                var hiddenAdderEvent = hiddenAdder == null ? null : hiddenAdder.DeclaringMember as Event;
                var hiddenCallerEvent = hiddenCaller == null ? null : hiddenCaller.DeclaringMember as Event;
                var hiddenRemoverEvent = hiddenRemover == null ? null : hiddenRemover.DeclaringMember as Event;

                hiddenEvent = hiddenAdderEvent;
                if (hiddenCallerEvent != null)
                    if (hiddenEvent == null ||
                        (hiddenCallerEvent.DeclaringType != null &&
                         hiddenCallerEvent.DeclaringType.IsDerivedFrom(hiddenEvent.DeclaringType)))
                        hiddenEvent = hiddenCallerEvent;
                if (hiddenRemoverEvent != null)
                    if (hiddenEvent == null ||
                        (hiddenRemoverEvent.DeclaringType != null &&
                         hiddenRemoverEvent.DeclaringType.IsDerivedFrom(hiddenEvent.DeclaringType)))
                        hiddenEvent = hiddenRemoverEvent;
                if (hiddenEvent == null)
                {
                    hiddenMember = NotSpecified;
                    return null;
                }

                hiddenMember = hiddenEvent;
                return hiddenEvent;
            }
            set { hiddenMember = value; }
        }

        public override Member OverriddenMember
        {
            get { return OverriddenEvent; }
            set { OverriddenEvent = (Event)value; }
        }

        protected Property overriddenEvent;
        public virtual Event OverriddenEvent
        {
            get
            {
                if (overriddenMember == NotSpecified) return null;
                var overriddenEvent = overriddenMember as Event;
                if (overriddenEvent != null) return overriddenEvent;

                var overriddenAdder = HandlerAdder == null ? null : HandlerAdder.OverriddenMethod;
                var overriddenCaller = HandlerCaller == null ? null : HandlerCaller.OverriddenMethod;
                var overriddenRemover = HandlerRemover == null ? null : HandlerRemover.OverriddenMethod;
                var overriddenAdderEvent = overriddenAdder == null ? null : overriddenAdder.DeclaringMember as Event;
                var overriddenCallerEvent = overriddenCaller == null ? null : overriddenCaller.DeclaringMember as Event;
                var overriddenRemoverEvent =
                    overriddenRemover == null ? null : overriddenRemover.DeclaringMember as Event;

                overriddenEvent = overriddenAdderEvent;
                if (overriddenCallerEvent != null)
                    if (overriddenEvent == null ||
                        (overriddenCallerEvent.DeclaringType != null &&
                         overriddenCallerEvent.DeclaringType.IsDerivedFrom(overriddenEvent.DeclaringType)))
                        overriddenEvent = overriddenCallerEvent;
                if (overriddenRemoverEvent != null)
                    if (overriddenEvent == null ||
                        (overriddenRemoverEvent.DeclaringType != null &&
                         overriddenRemoverEvent.DeclaringType.IsDerivedFrom(overriddenEvent.DeclaringType)))
                        overriddenEvent = overriddenRemoverEvent;
                if (overriddenEvent == null)
                {
                    overriddenMember = NotSpecified;
                    return null;
                }

                overriddenMember = overriddenEvent;
                return overriddenEvent;
            }
            set { overriddenMember = value; }
        }
#if FxCop
    internal override void GetName(MemberFormat options, StringBuilder name)
    {
      base.GetName(options, name);
      Method.AppendReturnType(options.ReturnType, this.HandlerType, name);
    }
#endif
    }

#if ExtendedRuntime || CodeContracts
    public abstract class MethodContractElement : Node
    {
        /// <summary>
        ///     Set via contract abbreviations to point to the def site of the abbreviation.
        ///     The regular context points to the use site.
        /// </summary>
        public SourceContext DefSite;

        public bool Inherited;
        public bool UsesModels;

        protected MethodContractElement(NodeType nodeType)
            : base(nodeType)
        {
        }

        public abstract Expression Assertion { get; }
#if !FxCop && ILOFFSETS
        public int ILOffset;

        // a string that a user wants associated with a particular element
        public Expression UserMessage;
        public Literal SourceConditionText;

#endif
    }

    public abstract class Requires : MethodContractElement
    {
        public Expression Condition;

        protected Requires()
            : base(NodeType.Requires)
        {
        }

        protected Requires(NodeType nodeType)
            : base(nodeType)
        {
        }

        protected Requires(NodeType nodeType, Expression expression)
            : base(nodeType)
        {
            Condition = expression;
        }

        public override Expression Assertion => Condition;
    }

    public class RequiresPlain : Requires
    {
        /// <summary>
        ///     If non-null, indicates that this is a Requires&lt;TException&gt; form throwing TException
        /// </summary>
        public TypeNode ExceptionType;

        public bool IsFromValidation;

        public RequiresPlain()
            : base(NodeType.RequiresPlain)
        {
        }

        public RequiresPlain(Expression expression)
            : base(NodeType.RequiresPlain, expression)
        {
        }

        public RequiresPlain(Expression expression, TypeNode texception)
            : base(NodeType.RequiresPlain, expression)
        {
            ExceptionType = texception;
        }

        public virtual bool IsWithException
        {
            get
            {
#if CLOUSOT
                Contract.Ensures(!Contract.Result<bool>() || ExceptionType != null);
#endif
                return ExceptionType != null;
            }
        }
    }

    public class OldExpression : Expression
    {
        public Expression expression;

        public OldExpression()
            : base(NodeType.OldExpression)
        {
        }

        public OldExpression(Expression expression)
            : base(NodeType.OldExpression)
        {
            this.expression = expression;
        }

        public int ShallowCopyUptoDimension { get; set; } = 0;
    }

    public class RequiresOtherwise : Requires
    {
        /// <summary>
        ///     The ThrowException can be a type reference (like "NullReferenceException")
        ///     or a value that would evaluate to something of an exception type.
        ///     (like new NullReferenceException("...") or C.f where the f is a static field
        ///     of class C whose type is an exception.
        /// </summary>
        public Expression ThrowException;

        public RequiresOtherwise()
            : base(NodeType.RequiresOtherwise)
        {
        }

        public RequiresOtherwise(Expression cond, Expression exc)
            : base(NodeType.RequiresOtherwise, cond)
        {
            ThrowException = exc;
        }
    }

    public abstract class Ensures : MethodContractElement
    {
        public Expression PostCondition;

        protected Ensures()
            : base(NodeType.Ensures)
        {
        }

        protected Ensures(NodeType nodeType)
            : base(nodeType)
        {
        }

        protected Ensures(NodeType nodeType, Expression expression)
            : base(nodeType)
        {
            PostCondition = expression;
        }

        public override Expression Assertion => PostCondition;
    }

    public class EnsuresNormal : Ensures
    {
        public EnsuresNormal()
            : base(NodeType.EnsuresNormal)
        {
        }

        public EnsuresNormal(Expression expression)
            : base(NodeType.EnsuresNormal, expression)
        {
        }
    }

    public class EnsuresExceptional : Ensures
    {
        public TypeNode Type;
        public TypeNode TypeExpression;
        public Expression Variable;

        public EnsuresExceptional()
            : base(NodeType.EnsuresExceptional)
        {
        }

        public EnsuresExceptional(Expression expression)
            : base(NodeType.EnsuresExceptional, expression)
        {
        }
    }

#if !CodeContracts
  public class ContractDeserializerContainer{
    public static IContractDeserializer ContractDeserializer;
  }
#endif
    public class MethodContract : Node
    {
        public Method /*!*/
            DeclaringMethod;

        public Method /*!*/
            OriginalDeclaringMethod;

        /// <summary>
        ///     Used when contract are extracted from code and the contract use delegates with closure objects. The
        ///     contractInitializer
        ///     block then contains the closure initialization and possibly other local initializations for delegate caches
        /// </summary>
        protected internal Block contractInitializer;

        /// <summary>
        ///     For contracts of constructors with closures, the initialization of the closure field holding "this" is postponed
        ///     until after the basector call. Post conditions may refer to this field in the contract copy of the closure and we
        ///     must thus initialize it explicitly after the base ctor call and before
        /// </summary>
        protected internal Block postPreamble;

        protected internal RequiresList requires;
        protected internal EnsuresList ensures;
        protected internal EnsuresList modelEnsures;

        /// <summary>
        ///     Used to support legacy if-then-throw and validator calls as contracts. Only used by rewriter.
        ///     The decompiled validations appear as Requires<![CDATA[<E>]]> on the requires list.
        ///     This list contains RequiresOtherwise and ordinary Requires in the proper order.
        /// </summary>
        protected internal RequiresList validations;

        protected internal ExpressionList modifies;
        protected internal bool? isPure;
        protected internal EnsuresList asyncEnsures;

#if ExtendedRuntime
    private static SourceContext SetContext(string/*!*/ filename, int startLine, int startCol, int endLine, int endCol, string/*!*/ sourceText) {
      SourceContext context;
      context.Document = new DocumentWithPrecomputedLineNumbers(filename, startLine, startCol, endLine, endCol);
      context.StartPos = 0;
      context.EndPos = sourceText.Length;
      context.Document.Text = new DocumentText(sourceText);
      context.Document.Text.Length = sourceText.Length;
      return context;
    }
    public static SourceContext GetSourceContext(AttributeNode/*!*/ attr) {
      string filename = "";
      int startLine = 0;
      int startCol = 0;
      int endLine = 0;
      int endCol = 0;
      string sourceText = "";
      if (attr.Expressions != null) {
        for (int expIndex = 1, expLen = attr.Expressions.Count; expIndex < expLen; expIndex++) {
          NamedArgument na = attr.Expressions[expIndex] as NamedArgument;
          if (na == null || na.Name == null) continue;
          Literal lit = na.Value as Literal;
          if (lit == null) continue;
          switch (na.Name.Name) {
            case "Filename":
            case "FileName":
              filename = (string)lit.Value; break;
            case "StartColumn": startCol = (int)lit.Value; break;
            case "StartLine": startLine = (int)lit.Value; break;
            case "EndColumn": endCol = (int)lit.Value; break;
            case "EndLine": endLine = (int)lit.Value; break;
            case "SourceText": sourceText = (string)lit.Value; break;
            default: break;
          }
        }
      }
      SourceContext ctx = SetContext(filename, startLine, startCol, endLine, endCol,sourceText);
      return ctx;
    }
#endif
        public Block ContractInitializer
        {
            get { return contractInitializer; }
            set { contractInitializer = value; }
        }

        public Block PostPreamble
        {
            get { return postPreamble; }
            set { postPreamble = value; }
        }

        public int RequiresCount
        {
            get
            {
#if CLOUSOT
                Contract.Ensures(Contract.Result<int>() == 0 || Requires != null);
                Contract.Ensures(Requires == null || Contract.Result<int>() == Requires.Count);
#endif
                var reqs = Requires;
                if (reqs == null) return 0;
                return reqs.Count;
            }
        }

        public int EnsuresCount
        {
            get
            {
#if CLOUSOT
                Contract.Ensures(Contract.Result<int>() == 0 || Ensures != null);
                Contract.Ensures(Ensures == null || Contract.Result<int>() == Ensures.Count);
#endif
                var ens = Ensures;
                if (ens == null) return 0;
                return ens.Count;
            }
        }

        public int ModelEnsuresCount
        {
            get
            {
#if CLOUSOT
                Contract.Ensures(Contract.Result<int>() == 0 || ModelEnsures != null);
                Contract.Ensures(ModelEnsures == null || Contract.Result<int>() == ModelEnsures.Count);
#endif
                var ens = ModelEnsures;
                if (ens == null) return 0;
                return ens.Count;
            }
        }

        public int AsyncEnsuresCount
        {
            get
            {
#if CLOUSOT
                Contract.Ensures(Contract.Result<int>() == 0 || AsyncEnsures != null);
                Contract.Ensures(AsyncEnsures == null || Contract.Result<int>() == AsyncEnsures.Count);
#endif
                var ens = AsyncEnsures;
                if (ens == null) return 0;
                return ens.Count;
            }
        }

        private int legacyValidations = -1;

        public bool HasLegacyValidations
        {
            get
            {
                if (legacyValidations < 0)
                {
                    legacyValidations = 0;
                    if (validations != null)
                        for (var i = 0; i < validations.Count; i++)
                        {
                            var ro = validations[i] as RequiresOtherwise;
                            if (ro != null) legacyValidations++;
                        }
                }

                return legacyValidations > 0;
            }
        }

        public int ValidationsCount
        {
            get
            {
                var val = Validations;
                if (val == null) return 0;
                return val.Count;
            }
        }

        public int ModifiesCount
        {
            get
            {
                var mods = Modifies;
                if (mods == null) return 0;
                return mods.Count;
            }
        }

        public RequiresList Requires
        {
            get
            {
#if CodeContracts
                return requires;
#else
        if (this.requires != null) return this.requires;
        RequiresList rs = this.requires = new RequiresList();
        if (this.DeclaringMethod != null){
          AttributeList attributes = this.DeclaringMethod.Attributes;
          if (attributes == null || attributes.Count == 0) return rs;
          IContractDeserializer ds = Cci.ContractDeserializerContainer.ContractDeserializer;
          if (ds != null){
            TypeNode t = this.DeclaringMethod.DeclaringType;
            Module savedCurrentAssembly = ds.CurrentAssembly;
            ds.CurrentAssembly = t == null ? null : t.DeclaringModule;
            for (int i = 0, n = attributes == null ? 0 : attributes.Count; i < n; i++){
              AttributeNode attr = attributes[i];
              if (attr == null) continue;
              MemberBinding mb = attr.Constructor as MemberBinding;
              if (mb != null){
                if (mb.BoundMember == null) continue;
                if (mb.BoundMember.DeclaringType != SystemTypes.RequiresAttribute) continue;
                if (attr.Expressions == null || !(attr.Expressions.Count > 0)) continue;

                Literal l = attr.Expressions[0] as Literal;
                if (l == null) continue;
                string s = (string) l.Value;
                Expression e = null;
                try {
                  e = ds.ParseContract(this,s,null);
                } catch {
                  continue; //return this.requires = new RequiresList();
                }
                if (e != null){
                  RequiresPlain rp = new RequiresPlain(e);
                  SourceContext ctx = MethodContract.GetSourceContext(attr);
                  e.SourceContext = ctx;
                  rs.Add(rp);
                }
              }
            }
            ds.CurrentAssembly = savedCurrentAssembly;
          }
        }
        return this.requires;
#endif
            }
            set { requires = value; }
        }

        public EnsuresList Ensures
        {
            get
            {
#if CodeContracts
                return ensures;
#else
        if (this.ensures != null) return this.ensures;
        EnsuresList es = this.ensures = new EnsuresList();
        if (this.DeclaringMethod != null){
          AttributeList attributes = this.DeclaringMethod.Attributes;
          if (attributes == null || attributes.Count == 0) return es;
          IContractDeserializer ds = Cci.ContractDeserializerContainer.ContractDeserializer;
          if (ds != null){
            TypeNode t = this.DeclaringMethod.DeclaringType;
            Module savedCurrentAssembly = ds.CurrentAssembly;
            ds.CurrentAssembly = t == null ? null : t.DeclaringModule;
            for (int i = 0, n = attributes == null ? 0 : attributes.Count; i < n; i++){
              AttributeNode attr = attributes[i];
              if (attr == null) continue;
              MemberBinding mb = attr.Constructor as MemberBinding;
              if (mb != null){
                if (mb.BoundMember == null) continue;
                if (mb.BoundMember.DeclaringType == SystemTypes.ThrowsAttribute) {
                  EnsuresExceptional ee = null;
                  Literal eeType = attr.Expressions[0] as Literal;
                  if (eeType == null) continue;
                  if (eeType.Type == SystemTypes.Type) {
                    TypeNode tn = (TypeNode)eeType.Value;
                    ee = new EnsuresExceptional();
                    ee.Type = tn;
                    es.Add(ee);
                  }
                  if (ee != null && attr.Expressions.Count > 1) {
                    Literal eeCondition = attr.Expressions[1] as Literal;
                    if (eeCondition != null) {
                      string eeString = (string)eeCondition.Value;
                      Expression deserializedEECondition = null;
                      try {
                        deserializedEECondition = ds.ParseContract(this, eeString, null);
                        Comprehension cmp = deserializedEECondition as Comprehension;
                        if (cmp != null) {
                          // See notes in Serializer for how this is represented
                          ComprehensionBinding cmpB = (ComprehensionBinding)cmp.BindingsAndFilters[0];
                          ee.Variable = cmpB.TargetVariable;
                          ee.PostCondition = cmp.Elements[0];
                        } else {
                          ee.PostCondition = deserializedEECondition;
                        }
                        SourceContext eeSourceContext = MethodContract.GetSourceContext(attr);
                        ee.PostCondition.SourceContext = eeSourceContext;
                      }
                      catch {
                        continue; //return this.ensures = new EnsuresList();
                      }
                    }
                  }
                }
                if (mb.BoundMember.DeclaringType != SystemTypes.EnsuresAttribute) continue;
                if (attr.Expressions == null || !(attr.Expressions.Count > 0)) continue;
                Literal l = attr.Expressions[0] as Literal;
                if (l == null) continue;
                string s = (string) l.Value;
                Expression e = null;
                try {
                  e = ds.ParseContract(this,s,null);
                } catch {
                  continue; //return this.ensures = new EnsuresList();
                }
                EnsuresNormal ens = new EnsuresNormal(e);
                SourceContext ctx = MethodContract.GetSourceContext(attr);
                e.SourceContext = ctx;
                es.Add(ens);
              }
            }
            ds.CurrentAssembly = savedCurrentAssembly;
          }
        }
        return this.ensures;
#endif
            }
            set { ensures = value; }
        }


        public EnsuresList AsyncEnsures
        {
            get { return asyncEnsures; }
            set { asyncEnsures = value; }
        }

        public EnsuresList ModelEnsures
        {
            get { return modelEnsures; }
            set { modelEnsures = value; }
        }

        /// <summary>
        ///     Contains the original requires contracts, including validations. Validations are not expanded
        ///     as in the Requires list. This list is used in the rewriter to emit code that uses the original
        ///     validation instructions rather than Requires(E)
        /// </summary>
        public RequiresList Validations
        {
            get { return validations; }
            set { validations = value; }
        }

        public bool IsPure
        {
            get
            {
                if (isPure.HasValue) return isPure.Value;
                if (DeclaringMethod != null)
                {
                    var attributes = DeclaringMethod.Attributes;
                    for (var i = 0; attributes != null && i < attributes.Count; i++)
                    {
                        var attr = attributes[i];
                        if (attr == null) continue;
                        if (attr.Type == null) continue;
                        if (attr.Type.Name == null) continue;
                        if (attr.Type.Name.Name == "PureAttribute")
                        {
                            isPure = true;
                            return true;
                        }
                    }
                }

                isPure = false;
                return false;
            }

            set { isPure = value; }
        }

        public ExpressionList Modifies
        {
            get
            {
#if CodeContracts
                return null;
#else
        if (this.modifies != null) return this.modifies;
        ExpressionList ms = this.modifies = new ExpressionList();
        if (this.DeclaringMethod != null){
          AttributeList attributes = this.DeclaringMethod.Attributes;
          if (attributes == null || attributes.Count == 0) return ms;
          IContractDeserializer ds = Cci.ContractDeserializerContainer.ContractDeserializer;
          if (ds != null){
            TypeNode t = this.DeclaringMethod.DeclaringType;
            Module savedCurrentAssembly = ds.CurrentAssembly;
            ds.CurrentAssembly = t == null ? null : t.DeclaringModule;
            for (int i = 0, n = attributes == null || attributes.Count == 0 ? 0 : attributes.Count; i < n; i++) {
              AttributeNode attr = attributes[i];
              if (attr == null) continue;
              MemberBinding mb = attr.Constructor as MemberBinding;
              if (mb != null){
                if (mb.BoundMember == null) continue;
                if (mb.BoundMember.DeclaringType != SystemTypes.ModifiesAttribute) continue;
                if (attr.Expressions == null || !(attr.Expressions.Count > 0)) continue;

                Literal l = attr.Expressions[0] as Literal;
                if (l == null) continue;
                string s = (string) l.Value;
                Expression e = ds.ParseContract(this,s,null);
                if (e == null) continue;
                SourceContext ctx = MethodContract.GetSourceContext(attr);
                e.SourceContext = ctx;
                ms.Add(e);
              }
            }
            ds.CurrentAssembly = savedCurrentAssembly;
          }
        }
        return this.modifies;
#endif
            }
            set { modifies = value; }
        }

        public MethodContract(Method /*!*/ declaringMethod)
            : base(NodeType.MethodContract)
        {
            DeclaringMethod = OriginalDeclaringMethod = declaringMethod;
        }

#if !CodeContracts
    public void CopyFrom(MethodContract sourceContract) {
      if ( sourceContract == null ) return;
      this.OriginalDeclaringMethod = sourceContract.OriginalDeclaringMethod;
      
      // Force deserialization (if necessary) to make sure sourceContract is fully populated
      // This is needed for LocalForResult: it is populated in the sourceContract only if the
      // postconditions have been deserialized.
      int dummy = sourceContract.RequiresCount;
      dummy = sourceContract.EnsuresCount + dummy;

      TypeNode t = this.DeclaringMethod.DeclaringType;
      Module m = t.DeclaringModule;
      Duplicator dup = new Duplicator(m,t);
      // Set up DuplicateFor table: all references to parameters from the source contract should be replaced
      // with references to the equivalent parameter from the target contract.
      // These references can be of type "Parameter" or "ParameterField".
      // Also, the local that holds the "result" of the method should be likewise mapped.
      // Also, the "this" parameter should be mapped.
      Method sourceMethod = sourceContract.DeclaringMethod;
      if (sourceMethod != null){
        MethodScope sourceScope = sourceMethod.Scope;
        Method targetMethod = this.DeclaringMethod;
        if (targetMethod != null){
          #region Map the self parameter
          if (sourceMethod.ThisParameter != null && targetMethod.ThisParameter != null){
            dup.DuplicateFor[sourceMethod.ThisParameter.UniqueKey] = targetMethod.ThisParameter;
          }
          #endregion
          #region Map the method parameters
          if (sourceMethod.Parameters != null && targetMethod.Parameters != null
            && sourceMethod.Parameters.Count == targetMethod.Parameters.Count){
            for (int i = 0, n = sourceMethod.Parameters.Count; i < n; i++){
              dup.DuplicateFor[sourceMethod.Parameters[i].UniqueKey] = targetMethod.Parameters[i];
            }
          }
          #endregion
          #region Map the ParameterFields
          MethodScope targetScope = targetMethod.Scope;
          if (sourceScope != null && targetScope != null){
            MemberList sourceScopeMembers = sourceScope.Members;
            for (int i = 0, n = sourceScopeMembers != null ? sourceScopeMembers.Count : 0; i < n; i++){
              ParameterField sourcePF = sourceScopeMembers[i] as ParameterField;
              if (sourcePF == null) continue;
              Parameter sourceP = sourcePF.Parameter;
              if (sourceP == null){ Debug.Assert(false); continue; }
              int index = sourceP.ParameterListIndex;
              if (targetMethod.Parameters == null || targetMethod.Parameters.Count <= index || index < 0){
                Debug.Assert(false); continue;
              }
              Parameter targetParameter = targetMethod.Parameters[index];
              Field f = targetScope.GetField(targetParameter.Name);
              if (f == null){ Debug.Assert(false); continue; }
              ParameterField targetPF = f as ParameterField;
              if (targetPF == null){ Debug.Assert(false); continue; }
              dup.DuplicateFor[sourcePF.UniqueKey] = targetPF;
            }
          }
          #endregion
        }
      }
      MethodContract duplicatedMC = dup.VisitMethodContract(sourceContract);
      duplicatedMC.isPure = sourceContract.IsPure; // force looking at attributes
      if (duplicatedMC != null && duplicatedMC.Requires != null && duplicatedMC.Requires.Count > 0) {
        RequiresList reqList = new RequiresList();
        for (int i = 0, n = duplicatedMC.Requires.Count; i< n; i++){
          Requires r = duplicatedMC.Requires[i];
          if (r != null) r.Inherited = true;
          reqList.Add(r);
        }
        if (this.requires != null)
        {
          foreach (Requires r in this.requires)
          {
            reqList.Add(r);
          }
        }
        this.Requires = reqList;
      }
      if (duplicatedMC != null && duplicatedMC.Ensures != null && duplicatedMC.Ensures.Count > 0 ) {
        EnsuresList enList = new EnsuresList();
        for(int i = 0, n = duplicatedMC.Ensures.Count; i < n; i++) {
          Ensures e = duplicatedMC.Ensures[i];
          if (e == null) continue;
          e.Inherited = true;
          enList.Add(e);
        }
        if (this.ensures != null)
        {
          foreach (Ensures e in this.ensures)
          {
            enList.Add(e);
          }
        }
        this.Ensures = enList;
      }
      if (duplicatedMC != null && duplicatedMC.Modifies != null && duplicatedMC.Modifies.Count > 0) {
        ExpressionList modlist = this.Modifies = (this.Modifies == null ? new ExpressionList() : this.Modifies);
        for (int i = 0, n = duplicatedMC.Modifies.Count; i < n; i++)
          modlist.Add(duplicatedMC.Modifies[i]);
      }
      this.contractInitializer = duplicatedMC.ContractInitializer;
      this.postPreamble = duplicatedMC.PostPreamble;
      return;
    }
#endif
    }

    /// <summary>
    ///     This is a method, as we need a binding for "this".
    /// </summary>
    public class Invariant : Method
    {
        public Expression Condition;
#if !FxCop && ILOFFSETS
        public int ILOffset;

        // a string that a user wants associated with a particular element
        public Literal UserMessage;
        public Literal SourceConditionText;
        public bool UsesModels;
#endif

#if !CodeContracts
    public Invariant(TypeNode declaringType, AttributeList attributes, Identifier name)
    {
      this.NodeType = NodeType.Invariant;
      this.attributes = attributes;
      this.DeclaringType = declaringType;
      this.Name = name;
      // this is called from the parser, so we have to avoid triggering CoreSystemType initialization.
      this.ReturnType = new TypeExpression(new Literal(TypeCode.Boolean), 0);
      this.ReturnTypeExpression = new TypeExpression(new Literal(TypeCode.Boolean), 0);
    }
#endif
        // called from Foxtrot
        public Invariant(TypeNode declaringType, Expression invariant, string name)
        {
            NodeType = NodeType.Invariant;
            DeclaringType = declaringType;
            Condition = invariant;
            if (name == null) name = "ObjectInvariant";
            Name = Identifier.For(name);
            ReturnType = new TypeExpression(new Literal(TypeCode.Boolean), 0);
        }
    }

#if ExtendedRuntime
  public class ModelfieldContract : Node {
    protected Field mf; //the modelfield this contract applies to (might be a temporary modelfield that stores unresolved override information)
    protected Property ifaceMf; //the interface modelfield this contract applies to.
    //invariant mf != null && ifaceMF == null || mf == null && ifaceMf != null;

    public Expression Witness = null;
    public bool HasExplicitWitness =
 false; //set to true if this modelfield has an explicitly specified witness. NOTE: Not serialized, i.e., not available in boogie!
    public ExpressionList/*!*/ SatisfiesList = new ExpressionList();            

    public TypeNode DeclaringType;

    public Member/*!*/ Modelfield { get { return this.mf == null ? (Member)this.ifaceMf : (Member)this.mf; } }
    public TypeNode/*!*/ ModelfieldType { get { return this.mf == null ? this.ifaceMf.Type : this.mf.Type; } }    
    private bool isOverride = false;
    public bool IsOverride {
      //slighty complicated to work both before and after serialization, and before and after update of modelfield reference if this contract is overriding a baseclass contract. 
      get {
        if (this.isOverride == true) return true;
        return !(this.Modelfield.DeclaringType == this.DeclaringType);
      }
      set {
        //requires value == true; (setting to false has no real meaning or effect)
        isOverride = value;
      }
    }

    private bool isSealed = false;  //set to true if modelfield itself is sealed (i.e., has the keyword).
    public bool IsSealed {
      get { if (this.isSealed) return true;
            if (this.DeclaringType == null) return false; //defensive check
            return this.DeclaringType.IsSealed;        
      }
      set { //requires value == true and the modelfield(contract) itself is sealed</summary>
        this.isSealed = value; }
    }    

    /// <summary>      
    /// ensures that the result is a new modelfieldcontract with an empty set of satisfies clauses and a default witness.
    /// ensures that the SourceContext of the result and the default witness are set to name.SourceContext.
    /// requires all attributes to be non-null
    /// </summary>    
    public ModelfieldContract(TypeNode declaringType, AttributeList attrs, TypeNode type, Identifier name, SourceContext sctx)
      : base(NodeType.ModelfieldContract)
    {
      this.DeclaringType = declaringType;            
      this.SourceContext = sctx;      
      if (declaringType is Class) {
        this.mf =
 new Field(declaringType, attrs, FieldFlags.Public, name, type, null); //note: if the modelfield has an override modifier, then mf is a placeholder. This will be signalled by a 'Private' flag.        
        this.mf.IsModelfield = true;
        this.mf.SourceContext = this.SourceContext;
      } else if (declaringType is Interface) {
        //Treat as a property with a getter that will return a modelfield from an implementing class        
        #region create a default abstract getter method getM
        Method getM =
 new Method(declaringType, new AttributeList(), new Identifier("get_" + name.Name), new ParameterList(), type, null);
        getM.SourceContext = this.SourceContext;
        getM.CallingConvention =
 CallingConventionFlags.HasThis; //needs to be changed when we want to allow static modelfields
        //Give getM [NoDefaultContract] so that it can easily be called in specs
        InstanceInitializer ndCtor = SystemTypes.NoDefaultContractAttribute.GetConstructor();        
        if (ndCtor != null)
          getM.Attributes.Add(new AttributeNode(new MemberBinding(null, ndCtor), null, AttributeTargets.Method));
        //Give getM "confined" (otherwise it still can't be called in specs, as it has NoDefaultContract)
        // That means make it [Pure][Reads(Reads.Owned)]
        InstanceInitializer pCtor = SystemTypes.PureAttribute.GetConstructor();        
        if (pCtor != null)
          getM.Attributes.Add(new AttributeNode(new MemberBinding(null, pCtor), null, AttributeTargets.Method));
        InstanceInitializer rCtor =
 SystemTypes.ReadsAttribute.GetConstructor(); // can use nullary ctor since default is confined
        if (rCtor != null)
          getM.Attributes.Add(new AttributeNode(new MemberBinding(null, rCtor), null, AttributeTargets.Method));
        //To the user, a modelfield on an interface is a field. Therefore, the user should be allowed to give it a [Rep] attribute.
        //But we treat the modelfield as a property, which is not a valid target for [Rep].  
        //We convert a [Rep] on the property to an [Owned] on the getter.                                        
        int nrOfAttrs = (attrs == null ? 0 : attrs.Count);
        for (int i = 0, j = 0; i < nrOfAttrs; i++) {          
          Identifier attrI =
 (attrs[i].Constructor as Identifier); //seems we need a HACK: Constructor has not been processed yet (by Looker).
          if (i == j && attrI != null && attrI.Name == "Rep") { //test i == j for slightly improved error handling on multiple [Rep]'s
            InstanceInitializer oCtor = SystemTypes.RepAttribute.GetConstructor();
            if (oCtor != null) { 
              getM.Attributes.Add(new AttributeNode(new MemberBinding(null, oCtor), null, AttributeTargets.Method));
              attrs[i] = null;
            }
          } else {
            attrs[j] = attrs[i];
            j = j + 1;
          }
        }
        declaringType.Members.Add(getM);
        getM.Flags =
 MethodFlags.Public | MethodFlags.Abstract | MethodFlags.NewSlot | MethodFlags.Virtual | MethodFlags.SpecialName | MethodFlags.HideBySig;        
        #endregion
        ifaceMf = new Property(declaringType, attrs, PropertyFlags.None, name, getM, null);
        ifaceMf.IsModelfield = true;
        ifaceMf.SourceContext = this.SourceContext;
        getM.DeclaringMember = ifaceMf;
      }      
    }
    
    /// <summary>
    /// ensures result.Modelfield == modelfield.    
    /// </summary>  
    public ModelfieldContract(TypeNode/* ! */ declaringType, Field/* ! */ modelfield)
      : base(NodeType.ModelfieldContract) {
      this.DeclaringType = declaringType;
      this.SourceContext = modelfield.SourceContext;      
      this.mf = modelfield;      
      if (modelfield.DeclaringType != declaringType)
        this.IsOverride = true;
    }    

    /// <summary>
    /// requires this.IsOverride == true;
    /// requires that newMf is a member of a superclass of mfC.DeclaringType;
    /// ensures this.Modelfield == newMf;
    /// this method can be used to update the modelfield of an overriding modelfieldcontract to the modelfield that is overridden.
    /// </summary>
    /// <param name="newMf">The overridden modelfield that this modelfieldContract applies to</param>
    public void UpdateModelfield(Field newMf) {
      this.mf = newMf;      
    }
    
    private ModelfieldContract nearestOverriddenContract; //null when this not an overriding contract (or when getNearestContractContract has not yet been called) 
    /// <summary>
    /// ensures: if this contract overrides a superclass contract, then result is the nearest overridden contract, else result == null. 
    /// </summary>       
    public ModelfieldContract NearestOverriddenContract {
      get {
        if (this.nearestOverriddenContract != null) return this.nearestOverriddenContract;

        if (this.mf == null) return null; //interface modelfieldContracts can't override
        if (!this.IsOverride) return null;
        #region scan superclasses until nearest overriden contract is found, then return that contract.
        for (Class currentClass = this.DeclaringType.BaseType as Class; currentClass != null; currentClass =
 currentClass.BaseClass) {
          foreach (ModelfieldContract currentMfC in currentClass.Contract.ModelfieldContracts) {
            if (currentMfC.Modelfield == this.mf) {
              this.nearestOverriddenContract = currentMfC;
              return this.nearestOverriddenContract;
            }
          }
        }
        Debug.Assert(false);  //an overridden contract should have been found and returned.  
        return this.nearestOverriddenContract;
        #endregion
      }
    }

  }

  public sealed class ModelfieldContractList {
    private ModelfieldContract[]/*!*/ elements;
    private int count = 0;
    public ModelfieldContractList() {
      this.elements = new ModelfieldContract[8];
      //^ base();
    }
    public ModelfieldContractList(int n) {
      this.elements = new ModelfieldContract[n];
      //^ base();
    }
    public ModelfieldContractList(params ModelfieldContract[] elements) {
      if (elements == null) elements = new ModelfieldContract[0];
      this.elements = elements;
      this.count = elements.Length;
      //^ base();
    }
    public void Add(ModelfieldContract element) {
      int n = this.elements.Length;
      int i = this.count++;
      if (i == n) {
        int m = n * 2; if (m < 8) m = 8;
        ModelfieldContract[] newElements = new ModelfieldContract[m];
        for (int j = 0; j < n; j++) newElements[j] = elements[j];
        this.elements = newElements;
      }
      this.elements[i] = element;
    }
    public ModelfieldContractList/*!*/ Clone() {
      ModelfieldContract[] elements = this.elements;
      int n = this.count;
      ModelfieldContractList result = new ModelfieldContractList(n);
      result.count = n;
      ModelfieldContract[] newElements = result.elements;
      for (int i = 0; i < n; i++)
        newElements[i] = elements[i];
      return result;
    }
    public int Count {
      get { return this.count; }
      set { this.count = value; }
    }
    [Obsolete("Use Count property instead.")]
    public int Length {
      get { return this.count; }
      set { this.count = value; }
    }
    public ModelfieldContract this[int index] {
      get {
        return this.elements[index];
      }
      set {
        this.elements[index] = value;
      }
    }
    public Enumerator GetEnumerator() {
      return new Enumerator(this);
    }
    public struct Enumerator {
      private int index;
      private readonly ModelfieldContractList/*!*/ list;
      public Enumerator(ModelfieldContractList/*!*/ list) {
        this.index = -1;
        this.list = list;
      }
      public ModelfieldContract Current {
        get {
          return this.list[this.index];
        }
      }
      public bool MoveNext() {
        return ++this.index < this.list.count;
      }
      public void Reset() {
        this.index = -1;
      }
    }
  }
#endif

    public class TypeContract : Node
    {
        public TypeNode DeclaringType;
#if ExtendedRuntime
    protected internal ModelfieldContractList modelfieldContracts;

    /// <summary> 
    /// Deserializes attr.Expressions[i] as expression E.
    /// requires attr.Expressions.Count > i;    
    /// requires this.DeclaringType != null;
    /// </summary> 
    /// <returns>E if succesfull, null otherwise.</returns>
    private Expression getIndexFromAttribute(AttributeNode attr, int i) {      
      Debug.Assert(attr != null && attr.Expressions.Count > i && this.DeclaringType != null); //something's wrong with the IL we are deserializing, should have generated an error while constructing the IL.
      IContractDeserializer ds = Cci.ContractDeserializerContainer.ContractDeserializer;
      if (ds == null) return null;
      ds.CurrentAssembly = this.DeclaringType.DeclaringModule;
      Literal l = attr.Expressions[i] as Literal;
      if (l == null) return null;
      string s = (string)l.Value;
      return ds.ParseContract(this, s, null);      
    }


    /// <summary>
    /// requires attr.Expressions.Count > 0
    /// ensures if attr.Expressions[0] can be deserialized as modelfield F, then:
    ///   if F key in contractLookup, then returns matching value, else returns new ModelfieldContract mfC for F
    /// else returns null
    /// ensures if new mfC created, then  (F, mfC) in contractLookup and mfC in this.ModelfieldContracts
    /// </summary>
    private ModelfieldContract getContractFor(AttributeNode attr, Dictionary<Field, ModelfieldContract> contractLookup) {
      Expression mfBinding = this.getIndexFromAttribute(attr, 0);
      
      //extract modelfield from mfBinding
      if (!(mfBinding is MemberBinding)) return null;
      Field modelfield = (mfBinding as MemberBinding).BoundMember as Field;
      if (modelfield == null) return null;

      //If this modelfield does not yet have a contract, then create one now and add <modelfield,mfC> to createdContracts       
      ModelfieldContract mfC = null;
      if (!contractLookup.TryGetValue(modelfield, out mfC)) {                
        mfC = new ModelfieldContract(this.DeclaringType, modelfield);        
        this.modelfieldContracts.Add(mfC);
        contractLookup.Add(modelfield, mfC);
      }

      return mfC;
    }

    public ModelfieldContractList/*!*/ ModelfieldContracts {
      get {
        if (this.modelfieldContracts == null) {                  
          this.modelfieldContracts = new ModelfieldContractList();
          #region deserialize the modelfieldcontracts if needed
          Dictionary<Field,ModelfieldContract> createdContracts =
 new Dictionary<Field,ModelfieldContract>(); //key = modelfield memberbinding, value = contract for that modelfield (if one was created already)
          if (this.DeclaringType != null) {                      
            foreach (AttributeNode attr in this.DeclaringType.Attributes) {                
              if (attr == null) continue;
              MemberBinding mb = attr.Constructor as MemberBinding;
              if (mb == null || mb.BoundMember == null) continue;
              if (mb.BoundMember.DeclaringType == SystemTypes.ModelfieldContractAttribute) {
                ModelfieldContract mfC = this.getContractFor(attr, createdContracts);
                Expression witness = this.getIndexFromAttribute(attr, 1);
                if (witness == null) continue;
                witness.SourceContext = MethodContract.GetSourceContext(attr);
                mfC.Witness = witness;
              } else if (mb.BoundMember.DeclaringType == SystemTypes.SatisfiesAttribute) {                
                ModelfieldContract mfC = this.getContractFor(attr, createdContracts);
                Expression satClause = this.getIndexFromAttribute(attr, 1);
                if (satClause == null) continue;
                satClause.SourceContext = MethodContract.GetSourceContext(attr);
                mfC.SatisfiesList.Add(satClause);              
              }                               
            }
          }
          #endregion
        }
        return this.modelfieldContracts;  
      }
      set { this.modelfieldContracts = value; }
    }
#endif

        public InvariantList InheritedInvariants;
        protected internal InvariantList invariants;
        public InvariantList Invariants
        {
            get
            {
#if CodeContracts
                return invariants;
#else
        if (this.invariants != null) return this.invariants;
        InvariantList invs = this.invariants = new InvariantList();
        if (this.DeclaringType != null){
          AttributeList attributes = this.DeclaringType.Attributes;
          IContractDeserializer ds = Cci.ContractDeserializerContainer.ContractDeserializer;
          if (ds != null){
            Module savedCurrentAssembly = ds.CurrentAssembly;
            ds.CurrentAssembly = this.DeclaringType == null ? null : this.DeclaringType.DeclaringModule;
            for (int i = 0, n = attributes == null || attributes.Count == 0 ? 0 : attributes.Count; i < n; i++){
              AttributeNode attr = attributes[i];
              if (attr == null) continue;
              MemberBinding mb = attr.Constructor as MemberBinding;
              if (mb != null){
                if (mb.BoundMember == null) continue;
                if (mb.BoundMember.DeclaringType != SystemTypes.InvariantAttribute) continue;
                if (attr.Expressions == null || !(attr.Expressions.Count > 0)) continue;

                Literal l = attr.Expressions[0] as Literal;
                if (l == null) continue;
                string s = (string) l.Value;
                Expression e = ds.ParseContract(this,s,null);
                if (e != null){
                  Invariant inv = new Invariant(this.DeclaringType,null,Identifier.For("invariant"+i));
                  SourceContext ctx = MethodContract.GetSourceContext(attr);
                  inv.SourceContext = ctx;
                  inv.Condition = e;
                  invs.Add(inv);
                }
              }
            }
            // Make the type contract look as it does when the type is compiled from source
            this.FramePropertyGetter = this.DeclaringType.GetMethod(Identifier.For("get_SpecSharp::FrameGuard"), null);
            this.InitFrameSetsMethod = this.DeclaringType.GetMethod(Identifier.For("SpecSharp::InitGuardSets"), null);
            this.InvariantMethod =
 this.DeclaringType.GetMethod(Identifier.For("SpecSharp::CheckInvariant"), CoreSystemTypes.Boolean);
            this.FrameProperty = this.DeclaringType.GetProperty(Identifier.For("SpecSharp::FrameGuard"));
            this.FrameField = this.DeclaringType.GetField(Identifier.For("SpecSharp::frameGuard"));
            ds.CurrentAssembly = savedCurrentAssembly;
          }
        }
        return this.invariants;
#endif
            }
            set { invariants = value; }
        }
#if ExtendedRuntime
    // when non-null, points to the method added to the DeclaringType that will have the invariants in its body
    // needed so when each invariant is visited, the proper environment can be set up for it.
    // NB: Dont' visit it as part of StandardVisitor
    public Field FrameField;
    public Property FrameProperty;
    public Method FramePropertyGetter;
    public Method InitFrameSetsMethod;

    public Method InvariantMethod;// when non-null, points to the method added to the DeclaringType that will have the invariants in its body

    /// <summary>
    /// Microsoft.Contracts.FrameGuardGetter implementation for this class.
    /// </summary>
    public Method GetFrameGuardMethod;
#endif
        /// <summary>
        ///     When types get constructed via the Reader, we let the Invariants be initialized on demand.
        ///     When the parser creates a type, we want the type contract to contain the empty invariant list
        ///     so that it won't grovel through the attributes on first access to Invariants.
        /// </summary>
        /// <param name="containingType"></param>
        public TypeContract(TypeNode containingType) : this(containingType, false)
        {
        }

        public TypeContract(TypeNode containingType, bool initInvariantList)
            : base(NodeType.TypeContract)
        {
            DeclaringType = containingType;
            if (initInvariantList) invariants = new InvariantList();
        }

        public int InvariantCount => Invariants == null ? 0 : Invariants.Count;
#if ExtendedRuntime
    public int ModelfieldContractCount { get { return ModelfieldContracts == null ? 0 : ModelfieldContracts.Count; } }
#endif
    }
#if !CodeContracts
  public interface IContractDeserializer{
    // when text is a requires, ensures, or modifies
    Expression ParseContract(MethodContract mc, string text, ErrorNodeList errors);
    // when text is an assertion or an assume in code
    Expression ParseContract(Method m, string text, ErrorNodeList errs);
    // when text is an invariant
    Expression ParseContract(TypeContract/*!*/ tc, string text, ErrorNodeList errs);
    Module CurrentAssembly { get; set; }
    ErrorNodeList ErrorList { get; set; }
  }
#endif
#endif
#if CodeContracts
    public class ExtraPDBInfo
    {
        private byte[] MD2;
        private byte[] asyncMethodInfo;
        private Reader reader;

        private List<MemoryStream> customDebugMetadataForCurrentMethod;
        private PdbFunction pdbfun;


        internal static ExtraPDBInfo Parse(uint parent, Method method, ISymUnmanagedReader symreader, Reader reader)
        {
            if (reader.pdbInfo != null)
            {
                var pdbfun = reader.pdbInfo.GetMethodInfo(parent);
                if (pdbfun != null)
                {
                    var result = new ExtraPDBInfo(reader);
                    result.pdbfun = pdbfun;
                    return result;
                }
            }

            return null;
        }

#if false
    static byte[] GetSymAttribute(uint parent, String name, ISymUnmanagedReader reader)
    {
      try
      {
        byte[] Data;
        uint cData = 0;
        reader.GetSymAttribute(parent, name, 0, ref cData, null);
        if (cData <= 0) return null;
        Data = new byte[cData];
        reader.GetSymAttribute(parent, name, cData, ref cData, Data);
        return Data;
      }
      catch
      {
        return null;
      }
    }
#endif

        internal void Write(uint token, ISymUnmanagedWriter writer, Ir2md ir2md)
        {
            customDebugMetadataForCurrentMethod = new List<MemoryStream>();

            MD2 = null; // don't just copy it over.
            SerializeReferenceToIteratorClass();
            SerializeReferenceToLastMethodWithUsingInfo();
            SerializeIteratorLocalScopes();
            SerializeCustomDebugMetadata();
            Write(token, writer, MD2, "MD2");
            SerializeSynchronizationInformation(ir2md);
            Write(token, writer, asyncMethodInfo, "asyncMethodInfo");
        }

        private void SerializeCustomDebugMetadata()
        {
            if (customDebugMetadataForCurrentMethod.Count == 0) return;
            var customMetadata = new MemoryStream();
            var cmw = new BinaryWriter(customMetadata);
            cmw.Write((byte)4); //version
            cmw.Write((byte)customDebugMetadataForCurrentMethod.Count); //count
            cmw.Align(4);
            foreach (var ms in customDebugMetadataForCurrentMethod)
                ms.WriteTo(customMetadata);
            MD2 = customMetadata.ToArray();
        }

        private void SerializeReferenceToLastMethodWithUsingInfo()
        {
            if (pdbfun == null) return;

            var customMetadata = new MemoryStream();
            var cmw = new BinaryWriter(customMetadata);
            cmw.Write((byte)4); //version
            cmw.Write((byte)1); //kind: ForwardInfo
            cmw.Align(4);
            cmw.Write((uint)12);
            cmw.Write(pdbfun.tokenOfMethodWhoseUsingInfoAppliesToThisMethod);
            customDebugMetadataForCurrentMethod.Add(customMetadata);
        }


        private void SerializeReferenceToIteratorClass()
        {
            if (pdbfun == null) return;
            var iteratorClassName = pdbfun.iteratorClass;
            if (iteratorClassName != null)
            {
                var customMetadata = new MemoryStream();
                var cmw = new BinaryWriter(customMetadata, true);
                cmw.Write((byte)4); //version
                cmw.Write((byte)4); //kind: ForwardIterator
                cmw.Align(4);
                var length = 10 + (uint)iteratorClassName.Length * 2;
                while (length % 4 > 0) length++;
                cmw.Write(length);
                cmw.Write(iteratorClassName, true);
                cmw.Align(4);
                customDebugMetadataForCurrentMethod.Add(customMetadata);
            }
        }

        private void SerializeIteratorLocalScopes()
        {
            if (pdbfun == null) return;
            var scopes = pdbfun.iteratorScopes;
            if (scopes == null || scopes.Count == 0) return;
            var numberOfScopes = (uint)scopes.Count;
            if (numberOfScopes == 0) return;
            var customMetadata = new MemoryStream();
            var cmw = new BinaryWriter(customMetadata);
            cmw.Write((byte)4); //version
            cmw.Write((byte)3); //kind: IteratorLocals
            cmw.Align(4);
            cmw.Write(12 + numberOfScopes * 8);
            cmw.Write(numberOfScopes);
            foreach (var scope in scopes)
            {
                // we don't know the scopes, so just show everything.
                // IF we don't emit a scope, debugger shows no locals at all.
                cmw.Write((uint)0);
                cmw.Write((uint)1000);
                //cmw.Write((uint)scope.Offset);
                //cmw.Write((uint)scope.Offset + scope.Length);
            }

            customDebugMetadataForCurrentMethod.Add(customMetadata);
        }

        private unsafe void Write(uint token, ISymUnmanagedWriter writer, byte[] data, string section)
        {
            if (data == null) return;
            fixed (byte* p = data)
            {
                writer.SetSymAttribute(token, section, (uint)data.Length, (IntPtr)p);
            }
        }

        private bool IsEmitted(Member member)
        {
            var asType = member as TypeNode;
            if (asType != null)
            {
                if (asType.DeclaringType != null)
                    return IsEmitted(asType.DeclaringType) && asType.DeclaringType.Members.Contains(asType);
                return asType.DeclaringModule.Types.Contains(asType);
            }

            var dT = member.DeclaringType;
            return IsEmitted(dT) && dT.Members.Contains(member);
        }

        private void SerializeSynchronizationInformation(Ir2md writer)
        {
            if (pdbfun == null || pdbfun.synchronizationInformation == null)
            {
                this.asyncMethodInfo = null;
                return;
            }

            var syncInfo = pdbfun.synchronizationInformation;
            var asyncMethodInfo = new MemoryStream();
            var cmw = new BinaryWriter(asyncMethodInfo);
            if (!IsEmitted(syncInfo.AsyncMethod))
            {
                this.asyncMethodInfo = null;
                return;
            }

            cmw.Write((uint)writer.GetMethodDefToken(syncInfo.AsyncMethod));
            cmw.Write(syncInfo.GeneratedCatchHandlerOffset);
            var syncPoints = syncInfo.SynchronizationPoints;
            cmw.Write((uint)syncPoints.Length);
            foreach (var syncPoint in syncPoints)
            {
                cmw.Write(syncPoint.SynchronizeOffset);
                var syncMethod = syncPoint.ContinuationMethod ?? syncInfo.MoveNextMethod;
                if (!IsEmitted(syncMethod))
                {
                    this.asyncMethodInfo = null;
                    return;
                }

                cmw.Write((uint)writer.GetMethodDefToken(syncMethod));
                cmw.Write(syncPoint.ContinuationOffset);
            }

            this.asyncMethodInfo = asyncMethodInfo.ToArray();
        }
#if false
    private void ReadMD2(Microsoft.Cci.Pdb.BitAccess bits)
    {
      byte version;
      bits.ReadUInt8(out version);
      if (version == 4)
      {
        byte count;
        bits.ReadUInt8(out count);
        bits.Align(4);
        while (count-- > 0)
          this.ReadCustomMetadata(bits);
      }

    }
    private void ReadCustomMetadata(Microsoft.Cci.Pdb.BitAccess bits)
    {
      int savedPosition = bits.Position;
      byte version;
      bits.ReadUInt8(out version);
      if (version != 4)
      {
        throw new PdbDebugException("Unknown custom metadata item version: {0}", version);
      }
      byte kind;
      bits.ReadUInt8(out kind);
      bits.Align(4);
      uint numberOfBytesInItem;
      bits.ReadUInt32(out numberOfBytesInItem);
      switch (kind)
      {
        case 0: this.ReadUsingInfo(bits); break;
        case 1: this.ReadForwardInfo(bits); break;
        case 2: break; // this.ReadForwardedToModuleInfo(bits); break;
        case 3: this.ReadIteratorLocals(bits); break;
        case 4: this.ReadForwardIterator(bits); break;
        default: throw new PdbDebugException("Unknown custom metadata item kind: {0}", kind);
      }
      bits.Position = savedPosition + (int)numberOfBytesInItem;
    }

    private void ReadForwardIterator(Microsoft.Cci.Pdb.BitAccess bits)
    {
      this.iteratorClass = bits.ReadString();
    }

    private void ReadIteratorLocals(Microsoft.Cci.Pdb.BitAccess bits)
    {
      uint numberOfLocals;
      bits.ReadUInt32(out numberOfLocals);
      this.iteratorScopes = new List<ILocalScope>((int)numberOfLocals);
      while (numberOfLocals-- > 0)
      {
        uint ilStartOffset;
        uint ilEndOffset;
        bits.ReadUInt32(out ilStartOffset);
        bits.ReadUInt32(out ilEndOffset);
        this.iteratorScopes.Add(new PdbIteratorScope(ilStartOffset, ilEndOffset - ilStartOffset));
      }
    }

    //private void ReadForwardedToModuleInfo(BitAccess bits) {
    //}

    private void ReadForwardInfo(Microsoft.Cci.Pdb.BitAccess bits)
    {
      bits.ReadUInt32(out this.tokenOfMethodWhoseUsingInfoAppliesToThisMethod);
      this.MethodWhoseUsingInfoAppliesToThisMethod =
 reader.pdbInfo.GetMethodFromPdbToken(this.tokenOfMethodWhoseUsingInfoAppliesToThisMethod);
    }

    private void ReadUsingInfo(Microsoft.Cci.Pdb.BitAccess bits)
    {
      ushort numberOfNamespaces;
      bits.ReadUInt16(out numberOfNamespaces);
      this.usingCounts = new ushort[numberOfNamespaces];
      for (ushort i = 0; i < numberOfNamespaces; i++)
      {
        bits.ReadUInt16(out this.usingCounts[i]);
      }
    }
#endif


        internal ExtraPDBInfo(Reader reader)
        {
            this.reader = reader;
        }
    }
#endif

    public class Method : Member
#if CodeContracts
        , IEquatable<Method>
#endif
    {
#if ExtendedRuntime
    /// <summary>
    /// Gets the first attribute of the given type in the attribute list of this method, or in the attribute list
    /// of the method's declaring member (if it exists).
    /// Returns null if none found.
    /// This should not be called until the AST containing this member has been processed to replace symbolic references
    /// to members with references to the actual members.
    /// </summary>
    public virtual AttributeNode GetAttributeFromSelfOrDeclaringMember (TypeNode attributeType) {
      AttributeNode a = base.GetAttribute(attributeType);
      if (a == null && this.DeclaringMember != null) {
        a = this.DeclaringMember.GetAttribute(attributeType);
      }
      return a;
    }
#endif
#if CodeContracts

        #region IEquatable<Method> members

        public bool Equals(Method other)
        {
            return this == other;
        }

        #endregion

#endif
#if ExtendedRuntime || CodeContracts
        public delegate void MethodContractProvider(Method /*!*/ method, object /*!*/ handle);

        internal MethodContractProvider ProvideContract;

        protected internal MethodContract /*?*/
            contract;

        internal static readonly MethodContract /*!*/
            DummyContract = new MethodContract(null);

        /// <summary>The preconditions, postconditions, and modifies clauses of this method.</summary>
        public virtual MethodContract /*?*/ Contract
        {
            get
            {
                if (contract != null) return contract;
                // There may be serialized contracts in the method's attributes.
                if (ProvideContract != null && ProviderHandle != null)
                    lock (Module.GlobalLock)
                    {
                        if (contract == null)
                        {
                            var provider = ProvideContract;
                            ProvideContract = null;
                            provider(this, ProviderHandle);
                        }
                    }

                return contract;
            }
            set
            {
                contract = value;
                if (value != null) contract.DeclaringMethod = this;
                ProvideContract = null;
            }
        }

        public void SetDelayedContract(MethodContractProvider provider)
        {
            CC.Contract.Assume(contract == null);
            if (ProvideContract != null)
                ProvideContract = (MethodContractProvider)Delegate.Combine(ProvideContract, provider);
            else
                ProvideContract = provider;
            contract = null;
        }
#endif
#if !MinimalReader && !CodeContracts
    public TypeNodeList ImplementedTypes;
    public TypeNodeList ImplementedTypeExpressions;
    public bool HasCompilerGeneratedSignature = true;
    public TypeNode ReturnTypeExpression;
    /// <summary>Provides a way to retrieve the parameters and local variables defined in this method given their names.</summary>
    public MethodScope Scope;
    public bool HasOutOfBandContract = false;
    protected TrivialHashtable/*!*/ Locals = null;
#endif
#if !FxCop
        public LocalList LocalList;
        protected SecurityAttributeList securityAttributes;
        /// <summary>Contains declarative security information associated with the type.</summary>
        public SecurityAttributeList SecurityAttributes
        {
            get
            {
                if (securityAttributes != null) return securityAttributes;
                if (attributes == null)
                {
                    var
                        al = Attributes; //Getting the type attributes also gets the security attributes, in the case of a type that was read in by the Reader
                    if (al != null) al = null;
                    if (securityAttributes != null) return securityAttributes;
                }

                return securityAttributes = new SecurityAttributeList(0);
            }
            set { securityAttributes = value; }
        }
#else
    internal SecurityAttributeList securityAttributes;
    public SecurityAttributeList SecurityAttributes{
      get{return this.securityAttributes;}
      internal set{this.securityAttributes = value;}
    }
    private LocalCollection locals;
    public LocalCollection Locals{
      get{
        if (locals == null) this.Body = this.Body;
        return this.locals;
      }
      internal set {
        this.locals = value;
      }
    }
    /// <summary>
    ///     Gets a value indicating whether the method is a property or event accessor.
    /// </summary>
    /// <value>
    ///     <see langword="true"/> if the <see cref="Method"/> is a property or event
    ///     accessor; otherwise, <see langword="false"/>.
    /// </value>
    /// <remarks>
    ///     <see cref="IsAccessor"/> returns <see langword="true"/> if 
    ///     <see cref="DeclaringMember"/> is not <see langword="null"/>.
    /// </remarks>
    public bool IsAccessor{
      get{return this.declaringMember != null;}
    }
    internal static bool EnforceMethodRepresentationCreationPolicy;
    internal static int PopulatedBodiesCount;
    internal static int PopulatedInstructionsCount;
#endif
        public delegate void MethodBodyProvider(Method /*!*/ method, object /*!*/ handle, bool asInstructionList);

        public MethodBodyProvider ProvideBody;
        public object ProviderHandle; //Opaque information to be used by the method body provider
#if ILOFFSETS
        public readonly int MethodToken;
        public Method(MethodBodyProvider provider, object handle, int methodToken)
            : this(provider, handle)
        {
            MethodToken = methodToken;
        }
#endif
        public Method()
            : base(NodeType.Method)
        {
        }

        public Method(MethodBodyProvider provider, object handle)
            : base(NodeType.Method)
        {
            ProvideBody = provider;
            ProviderHandle = handle;
        }

        public Method(TypeNode declaringType, AttributeList attributes, Identifier name, ParameterList parameters,
            TypeNode returnType, Block body)
            : base(declaringType, attributes, name, NodeType.Method)
        {
            this.body = body;
            Parameters = parameters; // important to use setter here.
            ReturnType = returnType;
        }

        public MethodFlags Flags { get; set; }

        public MethodImplFlags ImplFlags { get; set; }

        public MethodList ImplementedInterfaceMethods { get; set; }
#if !MinimalReader
        private MethodList implicitlyImplementedInterfaceMethods;

        /// <summary>
        ///     Computes the implicitly implemented methods for any method, not necessarily being compiled.
        /// </summary>
        public MethodList ImplicitlyImplementedInterfaceMethods
        {
            get
            {
                if (implicitlyImplementedInterfaceMethods == null)
                {
                    implicitlyImplementedInterfaceMethods = new MethodList();
                    // Degenerate case: interface methods don't implicitly implement anything
                    if (DeclaringType != null && DeclaringType.NodeType == NodeType.Interface)
                        return implicitlyImplementedInterfaceMethods;
                    // There are several reasons that this method cannot implicitly implement any interface method.
                    if ((ImplementedInterfaceMethods == null || ImplementedInterfaceMethods.Count == 0) && IsPublic &&
                        !IsStatic)
                    {
                        // It can implicitly implement an interface method for those interfaces that
                        // the method's type explicitly declares it implements
                        if (DeclaringType != null && DeclaringType.Interfaces != null)
                            foreach (var i in DeclaringType.Interfaces)
                            {
                                if (i == null) continue;
                                var match = i.GetExactMatchingMethod(this);
                                // But it cannot implicitly implement an interface method if there is
                                // an explicit implementation in the same type.
                                if (match != null && match.ReturnType.IsStructurallyEquivalentTo(ReturnType) &&
                                    !DeclaringType.ImplementsExplicitly(match))
                                    implicitlyImplementedInterfaceMethods.Add(match);
                            }

                        // It can implicitly implement an interface method if it overrides a base class
                        // method and *that* method implicitly implements the interface method.
                        // (Note: if this method's type does *not* explicitly declare that it implements
                        // the interface, then unless the method overrides a method that does, it is *not*
                        // used as an implicit implementation.)
                        if (OverriddenMethod != null)
                            foreach (var method in OverriddenMethod.ImplicitlyImplementedInterfaceMethods)
                                // But it cannot implicitly implement an interface method if there is
                                // an explicit implementation in the same type.
                                if (!DeclaringType.ImplementsExplicitly(method))
                                {
                                    var i = 0;
                                    var n = implicitlyImplementedInterfaceMethods.Count;
                                    while (i < n)
                                    {
                                        var alreadyImplementedMethod = implicitlyImplementedInterfaceMethods[i];
                                        if (alreadyImplementedMethod == method) break; // don't add it twice
                                        i++;
                                    }

                                    if (i == n)
                                        implicitlyImplementedInterfaceMethods.Add(method);
                                }
                    }
                }

                return implicitlyImplementedInterfaceMethods;
            }
            set { implicitlyImplementedInterfaceMethods = value; }
        }

        private MethodList shallowImplicitlyImplementedInterfaceMethods;
        /// <summary>
        ///     Computes the implicitly implemented methods (of interfaces of this type)
        /// </summary>
        public MethodList ShallowImplicitlyImplementedInterfaceMethods
        {
            get
            {
                if (shallowImplicitlyImplementedInterfaceMethods == null)
                {
                    shallowImplicitlyImplementedInterfaceMethods = new MethodList();
                    // Degenerate case: interface methods don't implicitly implement anything
                    if (DeclaringType != null && DeclaringType.NodeType == NodeType.Interface)
                        return shallowImplicitlyImplementedInterfaceMethods;
                    // There are several reasons that this method cannot implicitly implement any interface method.
                    if ((ImplementedInterfaceMethods == null || ImplementedInterfaceMethods.Count == 0) && IsPublic &&
                        !IsStatic)
                        // It can implicitly implement an interface method for those interfaces that
                        // the method's type explicitly declares it implements
                        if (DeclaringType != null && DeclaringType.Interfaces != null)
                            foreach (var i in DeclaringType.Interfaces)
                            {
                                if (i == null) continue;
                                var match = i.GetExactMatchingMethod(this);
                                // But it cannot implicitly implement an interface method if there is
                                // an explicit implementation in the same type.
                                if (match != null && match.ReturnType.IsStructurallyEquivalentTo(ReturnType) &&
                                    !DeclaringType.ImplementsExplicitly(match))
                                    shallowImplicitlyImplementedInterfaceMethods.Add(match);
                            }
                }

                return shallowImplicitlyImplementedInterfaceMethods;
            }
        }
#endif
        public CallingConventionFlags CallingConvention { get; set; }

        /// <summary>True if all local variables are to be initialized to default values before executing the method body.</summary>
        public bool InitLocals { get; set; } = true;

        /// <summary>True if this method is a template that conforms to the rules for a CLR generic method.</summary>
        public bool IsGeneric { get; set; }

        private ParameterList parameters;

        /// <summary>The parameters this method has to be called with.</summary>
        public ParameterList Parameters
        {
            get { return parameters; }
            set
            {
                parameters = value;
                if (value != null)
                    for (int i = 0, n = value.Count; i < n; i++)
                    {
                        var par = parameters[i];
                        if (par == null) continue;
                        par.DeclaringMethod = this;
                    }
            }
        }

        public int ParameterCount
        {
            get
            {
#if CodeContracts
                CC.Contract.Ensures(CC.Contract.Result<int>() >= 0);
                CC.Contract.Ensures(CC.Contract.Result<int>() == 0 || Parameters != null);
#endif
                if (parameters == null) return 0;
                return parameters.Count;
            }
        }

        public PInvokeFlags PInvokeFlags { get; set; } = PInvokeFlags.None;

        public Module PInvokeModule { get; set; }

        public string PInvokeImportName { get; set; }

        /// <summary>Attributes that apply to the return value of this method.</summary>
        public AttributeList ReturnAttributes { get; set; }

        public MarshallingInformation ReturnTypeMarshallingInformation { get; set; }

        /// <summary>The type of value that this method may return.</summary>
        public TypeNode ReturnType { get; set; }

        private Member declaringMember;

        /// <summary>Provides the declaring event or property of an accessor.</summary>
        public Member DeclaringMember
        {
            get
            {
                if (declaringMember == null && DeclaringType != null && !DeclaringType.membersBeingPopulated)
                {
                    var dummyMembers = DeclaringType.Members; //evaluate for side effect of filling in declaringMember
                }

                return declaringMember;
            }
            set { declaringMember = value; }
        }

        private This thisParameter;

        public This ThisParameter
        {
            get
            {
                if (thisParameter == null && !IsStatic && DeclaringType != null)
                {
                    if (DeclaringType.IsValueType)
                        ThisParameter = new This(DeclaringType.SelfInstantiation().GetReferenceType());
                    else
                        ThisParameter = new This(DeclaringType.SelfInstantiation());
                }

                return thisParameter;
            }
            set
            {
                if (value != null) value.DeclaringMethod = this;
                thisParameter = value;
            }
        }

        protected internal Block body;

        /// <summary>The instructions constituting the body of this method, in the form of a tree.</summary>
        public virtual Block Body
        {
            get
            {
                if (body != null) return body;
                if (ProvideBody != null && ProviderHandle != null)
                    lock (Module.GlobalLock)
                    {
                        if (body == null)
                        {
                            ProvideBody(this, ProviderHandle, false);
#if FxCop
              if (EnforceMethodRepresentationCreationPolicy && this.body.Statements.Count > 0)
                System.Threading.Interlocked.Increment(ref Method.PopulatedBodiesCount);
#endif
                        }
                    }

                return body;
            }
            set
            {
#if FxCop
        if (EnforceMethodRepresentationCreationPolicy && value == null && this.body != null && this.body.Statements.Count > 0)
          System.Threading.Interlocked.Decrement(ref Method.PopulatedBodiesCount);
#endif
                body = value;
            }
        }

        /// <summary>
        ///     A delegate that is called the first time Attributes is accessed, if non-null.
        ///     Provides for incremental construction of the type node.
        ///     Must not leave Attributes null.
        /// </summary>
        public MethodAttributeProvider ProvideMethodAttributes;

        /// <summary>
        ///     The type of delegates that fill in the Attributes property of the given method.
        /// </summary>
        public delegate void MethodAttributeProvider(Method /*!*/ method, object /*!*/ handle);

        public override AttributeList Attributes
        {
            get
            {
                if (attributes == null)
                {
                    if (ProvideMethodAttributes != null && ProviderHandle != null)
                        lock (Module.GlobalLock)
                        {
                            if (attributes == null)
                                ProvideMethodAttributes(this, ProviderHandle);
                        }
                    else
                        attributes = new AttributeList(0);
                }

                return attributes;
            }
            set { attributes = value; }
        }
#if FxCop
    internal void ClearBody(){
#else
        public void ClearBody()
        {
#endif
            lock (Module.GlobalLock)
            {
                Body = new Block(); // otherwise the code provider may repopulate it.
                Instructions = new InstructionList();
                ExceptionHandlers = null;
#if !FxCop
                LocalList = null;
#else
        this.Locals = null;
#endif
            }
        }

        protected string conditionalSymbol;
        protected bool doesNotHaveAConditionalSymbol;

        public string ConditionalSymbol
        {
            get
            {
                if (doesNotHaveAConditionalSymbol) return null;
                if (conditionalSymbol == null)
                    lock (this)
                    {
                        if (conditionalSymbol != null) return conditionalSymbol;
                        var condAttr = GetAttribute(SystemTypes.ConditionalAttribute);
                        if (condAttr != null && condAttr.Expressions != null && condAttr.Expressions.Count > 0)
                        {
                            var lit = condAttr.Expressions[0] as Literal;
                            if (lit != null)
                            {
                                conditionalSymbol = lit.Value as string;
                                if (conditionalSymbol != null) return conditionalSymbol;
                            }
                        }

                        doesNotHaveAConditionalSymbol = true;
                    }

                return conditionalSymbol;
            }
            set { conditionalSymbol = value; }
        }

        protected InstructionList instructions;
        /// <summary>The instructions constituting the body of this method, in the form of a linear list of Instruction nodes.</summary>
        public virtual InstructionList Instructions
        {
            get
            {
                if (instructions != null) return instructions;
                if (ProvideBody != null && ProviderHandle != null)
                    lock (Module.GlobalLock)
                    {
                        if (instructions == null)
                        {
                            ProvideBody(this, ProviderHandle, true);
#if FxCop
              if (EnforceMethodRepresentationCreationPolicy)
                  System.Threading.Interlocked.Increment(ref Method.PopulatedInstructionsCount);
#endif
                        }
                    }

                return instructions;
            }
            set
            {
#if FxCop
        if (EnforceMethodRepresentationCreationPolicy && this.instructions != null && value == null)
          System.Threading.Interlocked.Decrement(ref Method.PopulatedInstructionsCount);
#endif
                instructions = value;
            }
        }
#if !FxCop
        protected ExceptionHandlerList exceptionHandlers;
        public virtual ExceptionHandlerList ExceptionHandlers
        {
            get
            {
                if (exceptionHandlers != null) return exceptionHandlers;
                var dummy = Body;
                if (exceptionHandlers == null) exceptionHandlers = new ExceptionHandlerList(0);
                return exceptionHandlers;
            }
            set { exceptionHandlers = value; }
        }
#endif
#if !NoXml
        protected override Identifier GetDocumentationId()
        {
            if (Template != null) return Template.GetDocumentationId();
            var sb = new StringBuilder(DeclaringType.DocumentationId.ToString());
            sb[0] = 'M';
            sb.Append('.');
            if (NodeType == NodeType.InstanceInitializer)
            {
                sb.Append("#ctor");
            }
            else if (Name != null)
            {
                sb.Append(Name);
                if (TargetPlatform.GenericTypeNamesMangleChar != 0 && TemplateParameters != null &&
                    TemplateParameters.Count > 0)
                {
                    sb.Append(TargetPlatform.GenericTypeNamesMangleChar);
                    sb.Append(TargetPlatform.GenericTypeNamesMangleChar);
                    sb.Append(TemplateParameters.Count);
                }
            }

            var parameters = Parameters;
            for (int i = 0, n = parameters == null ? 0 : parameters.Count; i < n; i++)
            {
                var par = parameters[i];
                if (par == null || par.Type == null) continue;
                if (i == 0)
                    sb.Append('(');
                else
                    sb.Append(',');
                par.Type.AppendDocumentIdMangledName(sb, TemplateParameters, DeclaringType.TemplateParameters);
                if (i == n - 1)
                    sb.Append(')');
            }

            if (IsSpecialName && ReturnType != null && Name != null &&
                (Name.UniqueIdKey == StandardIds.opExplicit.UniqueIdKey ||
                 Name.UniqueIdKey == StandardIds.opImplicit.UniqueIdKey))
            {
                sb.Append('~');
                ReturnType.AppendDocumentIdMangledName(sb, TemplateParameters, DeclaringType.TemplateParameters);
            }

            return Identifier.For(sb.ToString());
        }
#endif
        protected internal string fullName;
        public override string /*!*/ FullName
        {
            get
            {
                if (fullName != null) return fullName;
                var sb = new StringBuilder();
                if (DeclaringType != null)
                {
                    sb.Append(DeclaringType.FullName);
                    sb.Append('.');
                    if (NodeType == NodeType.InstanceInitializer)
                        sb.Append("#ctor");
                    else if (Name != null)
                        sb.Append(Name);
                    var parameters = Parameters;
                    for (int i = 0, n = parameters == null ? 0 : parameters.Count; i < n; i++)
                    {
                        var par = parameters[i];
                        if (par == null || par.Type == null) continue;
                        if (i == 0)
                            sb.Append('(');
                        else
                            sb.Append(',');
                        sb.Append(par.Type.FullName);
                        if (i == n - 1)
                            sb.Append(')');
                    }
                }

                return fullName = sb.ToString();
            }
        }
#if ExtendedRuntime
    public override string HelpText {
      get {
        if (this.helpText != null)
          return this.helpText;
        StringBuilder sb = new StringBuilder(base.HelpText);
        // if there is already some help text, start the contract on a new line
        bool startWithNewLine = (sb.Length != 0);
        if (this.Contract != null){
          MethodContract mc = this.Contract;
          RequiresList rs = mc.Requires;
          if (rs != null && rs.Count == 0) { mc.Requires = null; rs = mc.Requires; }
          for (int i = 0, n = rs == null ? 0 : rs.Count; i < n; i++){
            Requires r = rs[i];
            if (r == null) continue;
            Expression e = r.Condition;
            if (e.SourceContext.StartPos < e.SourceContext.EndPos && e.SourceContext.SourceText != ""){
              if (startWithNewLine) sb.Append('\n');
              sb.Append("requires ");
              sb.Append(e.SourceContext.SourceText);
              sb.Append(";");
              startWithNewLine = true;
            }
          }
          EnsuresList es = mc.Ensures;
          if (es != null && es.Count == 0) { mc.Ensures = null; es = mc.Ensures; }
          if (es != null) {
            for (int i = 0, n = es.Count; i < n; i++) {
              Ensures e = es[i];
              if (e == null) continue;
              if (startWithNewLine) sb.Append('\n');
              EnsuresExceptional ee = e as EnsuresExceptional;
              if (ee != null) {
                sb.Append("throws ");
                if (ee.Variable != null) { sb.Append("("); }
                sb.Append(ee.Type.Name.ToString());
                if (ee.Variable != null) {
                  sb.Append(" ");
                  sb.Append(ee.Variable.SourceContext.SourceText);
                  sb.Append(")");
                }
              }
              if (e.PostCondition != null) {
                if (ee != null) {
                  sb.Append(" ");
                }
                Expression cond = e.PostCondition;
                sb.Append("ensures ");
                sb.Append(cond.SourceContext.SourceText);
              }
              sb.Append(";");
              startWithNewLine = true;
            }
          }
          ExpressionList exps = mc.Modifies;
          // Force deserialization in case that is needed
          if (exps != null && exps.Count == 0) { mc.Modifies = null; exps = mc.Modifies; }
          if (exps != null) {
            for (int i = 0, n = exps.Count; i < n; i++) {
              Expression mod = exps[i];
              if (mod != null && mod.SourceContext.StartPos < mod.SourceContext.EndPos && mod.SourceContext.SourceText != "") {
                if (startWithNewLine) sb.Append('\n');
                sb.Append("modifies ");
                sb.Append(mod.SourceContext.SourceText);
                sb.Append(";");
                startWithNewLine = true;
              }
            }
          }
        }
        return this.helpText = sb.ToString();
      }
      set {
        base.HelpText = value;
      }
    }
#endif
        public virtual string GetUnmangledNameWithoutTypeParameters()
        {
            return GetUnmangledNameWithoutTypeParameters(false);
        }

        public virtual string GetUnmangledNameWithoutTypeParameters(bool omitParameterTypes)
        {
            var sb = new StringBuilder();
            if (NodeType == NodeType.InstanceInitializer)
            {
                sb.Append("#ctor");
            }
            else if (Name != null)
            {
                var name = Name.ToString();
                var lastDot = name.LastIndexOf('.');
                var lastMangle = name.LastIndexOf('>');
                // explicit interface method overrides will have typenames in
                // their method name, which may also contain type parameters
                if (lastMangle < lastDot)
                    lastMangle = -1;
                if (lastMangle > 0)
                    sb.Append(name.Substring(0, lastMangle + 1));
                else
                    sb.Append(name);
            }

            if (omitParameterTypes) return sb.ToString();
            var parameters = Parameters;
            for (int i = 0, n = parameters == null ? 0 : parameters.Count; i < n; i++)
            {
                var par = parameters[i];
                if (par == null || par.Type == null) continue;
                if (i == 0)
                    sb.Append('(');
                else
                    sb.Append(',');
                sb.Append(par.Type.GetFullUnmangledNameWithTypeParameters());
                if (i == n - 1)
                {
#if !MinimalReader
                    if (IsVarArg) sb.Append(", __arglist");
#endif
                    sb.Append(')');
                }
            }

            return sb.ToString();
        }

        public virtual string GetUnmangledNameWithTypeParameters()
        {
            return GetUnmangledNameWithTypeParameters(false);
        }

        public virtual string GetUnmangledNameWithTypeParameters(bool omitParameterTypes)
        {
            var sb = new StringBuilder();
            sb.Append(GetUnmangledNameWithoutTypeParameters(true));
            var templateParameters = TemplateParameters;
            for (int i = 0, n = templateParameters == null ? 0 : templateParameters.Count; i < n; i++)
            {
                var tpar = templateParameters[i];
                if (tpar == null) continue;
                if (i == 0)
                    sb.Append('<');
                else
                    sb.Append(',');
                sb.Append(tpar.Name);
                if (i == n - 1)
                    sb.Append('>');
            }

            if (omitParameterTypes) return sb.ToString();
            var parameters = Parameters;
            for (int i = 0, n = parameters == null ? 0 : parameters.Count; i < n; i++)
            {
                var par = parameters[i];
                if (par == null || par.Type == null) continue;
                if (i == 0)
                    sb.Append('(');
                else
                    sb.Append(',');
                sb.Append(par.Type.GetFullUnmangledNameWithTypeParameters());
                if (i == n - 1)
                    sb.Append(')');
            }

            return sb.ToString();
        }

        public virtual string GetFullUnmangledNameWithTypeParameters()
        {
            return GetFullUnmangledNameWithTypeParameters(false);
        }

        public virtual string GetFullUnmangledNameWithTypeParameters(bool omitParameterTypes)
        {
            var sb = new StringBuilder();
            sb.Append(DeclaringType.GetFullUnmangledNameWithTypeParameters());
            sb.Append('.');
            sb.Append(GetUnmangledNameWithTypeParameters());
            return sb.ToString();
        }

        public static MethodFlags GetVisibilityUnion(Method m1, Method m2)
        {
            if (m1 == null && m2 != null) return m2.Flags & MethodFlags.MethodAccessMask;
            if (m2 == null && m1 != null) return m1.Flags & MethodFlags.MethodAccessMask;
            if (m1 == null || m2 == null) return MethodFlags.CompilerControlled;
            return GetVisibilityUnion(m1.Flags, m2.Flags);
        }

        public static MethodFlags GetVisibilityUnion(MethodFlags vis1, MethodFlags vis2)
        {
            vis1 &= MethodFlags.MethodAccessMask;
            vis2 &= MethodFlags.MethodAccessMask;
            switch (vis1)
            {
                case MethodFlags.Public:
                    return MethodFlags.Public;
                case MethodFlags.Assembly:
                    switch (vis2)
                    {
                        case MethodFlags.Public:
                            return MethodFlags.Public;
                        case MethodFlags.FamORAssem:
                        case MethodFlags.Family:
                            return MethodFlags.FamORAssem;
                        default:
                            return vis1;
                    }
                case MethodFlags.FamANDAssem:
                    switch (vis2)
                    {
                        case MethodFlags.Public:
                            return MethodFlags.Public;
                        case MethodFlags.Assembly:
                            return MethodFlags.Assembly;
                        case MethodFlags.FamORAssem:
                            return MethodFlags.FamORAssem;
                        case MethodFlags.Family:
                            return MethodFlags.Family;
                        default:
                            return vis1;
                    }
                case MethodFlags.FamORAssem:
                    switch (vis2)
                    {
                        case MethodFlags.Public:
                            return MethodFlags.Public;
                        default:
                            return vis1;
                    }
                case MethodFlags.Family:
                    switch (vis2)
                    {
                        case MethodFlags.Public:
                            return MethodFlags.Public;
                        case MethodFlags.FamORAssem:
                        case MethodFlags.Assembly:
                            return MethodFlags.FamORAssem;
                        default:
                            return vis1;
                    }
                default:
                    return vis2;
            }
        }
#if !NoReflection
        public virtual object Invoke(object targetObject, params object[] arguments)
        {
            var methInfo = GetMethodInfo();
            if (methInfo == null) return null;
            return methInfo.Invoke(targetObject, arguments);
        }

        public virtual Literal Invoke(Literal /*!*/ targetObject, params Literal[] arguments)
        {
            var n = arguments == null ? 0 : arguments.Length;
            var args = n == 0 ? null : new object[n];
            if (args != null && arguments != null)
                for (var i = 0; i < n; i++)
                {
                    var lit = arguments[i];
                    args[i] = lit == null ? null : lit.Value;
                }

            return new Literal(Invoke(targetObject.Value, args));
        }
#endif
#if !MinimalReader
        protected bool isNormalized;
        public virtual bool IsNormalized
        {
            get
            {
                if (isNormalized) return true;
                if (DeclaringType == null || SourceContext.Document != null) return false;
                return isNormalized = DeclaringType.IsNormalized;
            }
            set { isNormalized = value; }
        }
#endif
        public virtual bool IsAbstract => (Flags & MethodFlags.Abstract) != 0;

        public override bool IsAssembly => (Flags & MethodFlags.MethodAccessMask) == MethodFlags.Assembly;

        public override bool IsCompilerControlled =>
            (Flags & MethodFlags.MethodAccessMask) == MethodFlags.CompilerControlled;

        public virtual bool IsExtern => (Flags & MethodFlags.PInvokeImpl) != 0 ||
                                        (ImplFlags & (MethodImplFlags.Runtime | MethodImplFlags.InternalCall)) != 0;

        public override bool IsFamily => (Flags & MethodFlags.MethodAccessMask) == MethodFlags.Family;

        public override bool IsFamilyAndAssembly => (Flags & MethodFlags.MethodAccessMask) == MethodFlags.FamANDAssem;
        public override bool IsFamilyOrAssembly => (Flags & MethodFlags.MethodAccessMask) == MethodFlags.FamORAssem;
        public virtual bool IsFinal => (Flags & MethodFlags.Final) != 0;
#if !MinimalReader
        public virtual bool IsInternalCall => (ImplFlags & MethodImplFlags.InternalCall) != 0;
#endif
        public override bool IsPrivate => (Flags & MethodFlags.MethodAccessMask) == MethodFlags.Private;

        public override bool IsPublic => (Flags & MethodFlags.MethodAccessMask) == MethodFlags.Public;
        public override bool IsSpecialName => (Flags & MethodFlags.SpecialName) != 0;

        public override bool IsStatic => (Flags & MethodFlags.Static) != 0;
        /// <summary>
        ///     True if this method can in principle be overridden by a method in a derived class.
        /// </summary>
        public virtual bool IsVirtual => (Flags & MethodFlags.Virtual) != 0;
#if !MinimalReader
        public virtual bool IsNonSealedVirtual =>
            (Flags & MethodFlags.Virtual) != 0 && (Flags & MethodFlags.Final) == 0 &&
            (DeclaringType == null || (DeclaringType.Flags & TypeFlags.Sealed) == 0);

        public virtual bool IsVirtualAndNotDeclaredInStruct => (Flags & MethodFlags.Virtual) != 0 &&
                                                               (DeclaringType == null || !(DeclaringType is Struct));
#endif
        public override bool IsVisibleOutsideAssembly
        {
            get
            {
                if (DeclaringType != null && !DeclaringType.IsVisibleOutsideAssembly) return false;
                switch (Flags & MethodFlags.MethodAccessMask)
                {
                    case MethodFlags.Public:
                        return true;
                    case MethodFlags.Family:
                    case MethodFlags.FamORAssem:
                        if (DeclaringType != null && !DeclaringType.IsSealed) return true;
                        goto default;
                    default:
                        for (int i = 0, n = ImplementedInterfaceMethods == null ? 0 : ImplementedInterfaceMethods.Count;
                             i < n;
                             i++)
                        {
                            var m = ImplementedInterfaceMethods[i];
                            if (m == null) continue;
                            if (m.DeclaringType != null && !m.DeclaringType.IsVisibleOutsideAssembly) continue;
                            if (m.IsVisibleOutsideAssembly) return true;
                        }

                        return false;
                }
            }
        }
#if ExtendedRuntime
    /// <summary>
    /// VERY IMPORTANT! This property is true only for those pure methods that are *neither*
    /// confined [Reads(Owned)] *nor* state independent [Reads(Nothing)]
    /// </summary>
    public bool IsPure{
      get{
        AttributeNode attr = this.GetAttributeFromSelfOrDeclaringMember(SystemTypes.PureAttribute);
        if (attr == null) return false; // no [Pure] at all
        ExpressionList exprs = attr.Expressions;
        if (exprs != null && 0 < exprs.Count) {
          Literal lit = exprs[0] as Literal;
          if (lit != null && (lit.Value is bool)) {
            bool val = (bool)lit.Value;
            if (!val) return false; // [Pure(false)]
          }
        }
        // pure methods must be marked as [Pure] *and* [Reads(Everything)]
        AttributeNode a = this.GetAttributeFromSelfOrDeclaringMember(SystemTypes.ReadsAttribute);
        if (a == null) return false;
        Literal l = a.GetPositionalArgument(0) as Literal;
        if (l == null) return false; // because default ctor for Reads sets it to "Owned", not "Everything"
        Microsoft.Contracts.ReadsAttribute.Reads r = (Microsoft.Contracts.ReadsAttribute.Reads)l.Value;
        return r == Microsoft.Contracts.ReadsAttribute.Reads.Everything;
      }
    }
    public bool ApplyDefaultContract {
      get{
        return this.GetAttribute(SystemTypes.NoDefaultContractAttribute) == null;
      }
    }
#endif
#if ExtendedRuntime || CodeContracts
        public bool IsPropertyGetter
        {
            get
            {
                if (DeclaringMember == null) return false;
                var p = DeclaringMember as Property;
                if (p == null) return false;
                if (p.Getter == this) return true;
                if (Template != null)
                {
                    p = Template.DeclaringMember as Property;
                    if (p != null) return p.Getter == Template;
                }

                return false;
            }
        }

        public bool IsPropertySetter
        {
            get
            {
                if (DeclaringMember == null) return false;
                var p = DeclaringMember as Property;
                if (p == null) return false;
                if (p.Setter == this) return true;
                if (Template != null)
                {
                    p = Template.DeclaringMember as Property;
                    if (p != null) return p.Setter == Template;
                }

                return false;
            }
        }
#endif
#if ExtendedRuntime
    public bool IsConfined {
      get{
        if (this.DeclaringType is Struct || this.IsStatic) return false; // structs can't own anything

        // default: instance property getters are confined
        if (this.ApplyDefaultContract && this.IsPropertyGetter &&
           this.GetAttributeFromSelfOrDeclaringMember(SystemTypes.PureAttribute) == null) {
          return true;
        }
        
        // TODO: Remove the next if test after LKG > 11215 (20 December 2007)
        if (this.GetAttributeFromSelfOrDeclaringMember(SystemTypes.ConfinedAttribute) != null) return true;

        AttributeNode attr = this.GetAttributeFromSelfOrDeclaringMember(SystemTypes.PureAttribute);
        if (attr == null)
          return false; // not pure at all, so how could it be confined?
        // Make sure it isn't [Pure(false)]
        ExpressionList exprs = attr.Expressions;
        if (exprs != null && 0 < exprs.Count) {
          Literal lit = exprs[0] as Literal;
          if (lit != null && (lit.Value is bool)) {
            bool val = (bool)lit.Value;
            if (!val) return false; // [Pure(false)]
          }
        }

        AttributeNode a = this.GetAttributeFromSelfOrDeclaringMember(SystemTypes.ReadsAttribute);
        if (a == null) {
          // [Pure] by itself means confined on an instance method in a class
          // otherwise, must specify Reads(Owned) in order to be considered confined
          return !this.IsStatic;
        }
        Literal l = a.GetPositionalArgument(0) as Literal;
        if (l == null) return true; // because default ctor for Reads sets it that way
        Microsoft.Contracts.ReadsAttribute.Reads r = (Microsoft.Contracts.ReadsAttribute.Reads)l.Value;
        return r == Microsoft.Contracts.ReadsAttribute.Reads.Owned;
      }
    }
    public bool IsWriteConfined {
      get {
        return this.GetAttributeFromSelfOrDeclaringMember(SystemTypes.WriteConfinedAttribute) != null
            || IsConfined || IsStateIndependent;
      }
    }
    public bool IsStateIndependent{
      get{
        if (this.ApplyDefaultContract && this.IsPropertyGetter && (this.DeclaringType is Struct || this.IsStatic) &&
           this.GetAttributeFromSelfOrDeclaringMember(SystemTypes.PureAttribute) == null) {
          return true;
        }
        
        // TODO: Remove the next if test after LKG > 11215 (20 December 2007)
        if (this.GetAttributeFromSelfOrDeclaringMember(SystemTypes.StateIndependentAttribute) != null) return true;

        AttributeNode attr = this.GetAttributeFromSelfOrDeclaringMember(SystemTypes.PureAttribute);
        if (attr == null)
          return false; // not pure at all, so how could it be confined?
        // Make sure it isn't [Pure(false)]
        ExpressionList exprs = attr.Expressions;
        if (exprs != null && 0 < exprs.Count) {
          Literal lit = exprs[0] as Literal;
          if (lit != null && (lit.Value is bool)) {
            bool val = (bool)lit.Value;
            if (!val) return false; // [Pure(false)]
          }
        }

        AttributeNode a = this.GetAttributeFromSelfOrDeclaringMember(SystemTypes.ReadsAttribute);
        if (a == null) {
          // [Pure] by itself means state independent on an instance method in a struct
          // or any a static method (either in a struct or in a class)
          // otherwise, must specify Reads(Nothing) in order to be considered state independent
          return this.IsStatic || this.DeclaringType is Struct;
        }
        Literal l = a.GetPositionalArgument(0) as Literal;
        if (l == null) return false; // because default ctor for Reads sets it that way
        Microsoft.Contracts.ReadsAttribute.Reads r = (Microsoft.Contracts.ReadsAttribute.Reads)l.Value;
        return r == Microsoft.Contracts.ReadsAttribute.Reads.Nothing;
      }
    }
#endif
#if !MinimalReader
        public bool IsVarArg => (CallingConvention & CallingConventionFlags.VarArg) != 0;

        // whether this is a FieldInitializerMethod (declared in Sing#)
        public virtual bool IsFieldInitializerMethod => false;
#endif
        public override Member HiddenMember
        {
            get { return HiddenMethod; }
            set { HiddenMethod = (Method)value; }
        }

        public virtual Method HiddenMethod
        {
            get
            {
                if (hiddenMember == NotSpecified) return null;
                var hiddenMethod = hiddenMember as Method;
                if (hiddenMethod != null) return hiddenMethod;
                if (ProvideBody == null) return null;
                if (IsVirtual && (Flags & MethodFlags.VtableLayoutMask) != MethodFlags.NewSlot) return null;
                var baseType = DeclaringType.BaseType;
                while (baseType != null)
                {
                    var baseMembers = baseType.GetMembersNamed(Name);
                    if (baseMembers != null)
                        for (int i = 0, n = baseMembers.Count; i < n; i++)
                        {
                            var bmeth = baseMembers[i] as Method;
                            if (bmeth == null) continue;
                            if (!bmeth.ParametersMatch(Parameters))
                            {
                                if (TemplateParameters != null && TemplateParametersMatch(bmeth.TemplateParameters))
                                {
                                    if (!bmeth.ParametersMatchStructurally(Parameters)) continue;
                                }
                                else
                                {
                                    continue;
                                }
                            }

                            hiddenMethod = bmeth;
                            goto done;
                        }

                    baseType = baseType.BaseType;
                }

                done:
                if (hiddenMethod == null)
                {
                    hiddenMember = NotSpecified;
                    return null;
                }

                hiddenMember = hiddenMethod;
                return hiddenMethod;
            }
            set { hiddenMember = value; }
        }

        public override Member OverriddenMember
        {
            get { return OverriddenMethod; }
            set { OverriddenMethod = (Method)value; }
        }

        public virtual Method OverriddenMethod
        {
            get
            {
                if ((Flags & MethodFlags.VtableLayoutMask) == MethodFlags.NewSlot) return null;
                if (overriddenMember == NotSpecified) return null;
                var overriddenMethod = overriddenMember as Method;
                if (overriddenMethod != null) return overriddenMethod;
                if (ProvideBody == null) return null;
                if (!IsVirtual) return null;
                var baseType = DeclaringType.BaseType;
                while (baseType != null)
                {
                    var baseMembers = baseType.GetMembersNamed(Name);
                    if (baseMembers != null)
                        for (int i = 0, n = baseMembers.Count; i < n; i++)
                        {
                            var bmeth = baseMembers[i] as Method;
                            if (bmeth == null) continue;
                            if (!bmeth.ParametersMatch(Parameters))
                            {
                                if (TemplateParameters != null && TemplateParametersMatch(bmeth.TemplateParameters))
                                {
                                    if (!bmeth.ParametersMatchStructurally(Parameters)) continue;
                                }
                                else
                                {
                                    continue;
                                }
                            }

                            overriddenMethod = bmeth;
                            goto done;
                        }

                    baseType = baseType.BaseType;
                }

                done:
                if (overriddenMethod == null)
                {
                    overriddenMember = NotSpecified;
                    return null;
                }

                overriddenMember = overriddenMethod;
                return overriddenMethod;
            }
            set { overriddenMember = value; }
        }
#if !NoReflection
        public static Method GetMethod(Reflection.MethodInfo methodInfo)
        {
            if (methodInfo == null) return null;
#if WHIDBEY
            if (methodInfo.IsGenericMethod && !methodInfo.IsGenericMethodDefinition)
                try
                {
                    var template = GetMethod(methodInfo.GetGenericMethodDefinition());
                    if (template == null) return null;
                    var templateArguments = new TypeNodeList();
                    foreach (var arg in methodInfo.GetGenericArguments())
                        templateArguments.Add(TypeNode.GetTypeNode(arg));
                    return template.GetTemplateInstance(template.DeclaringType, templateArguments);
                }
                catch
                {
                    //TODO: log error
                    return null;
                }
#endif
            var tn = TypeNode.GetTypeNode(methodInfo.DeclaringType);
            if (tn == null) return null;
            var paramInfos = methodInfo.GetParameters();
            var n = paramInfos == null ? 0 : paramInfos.Length;
            var parameterTypes = new TypeNode[n];
            for (var i = 0; i < n; i++)
            {
                var param = paramInfos[i];
                if (param == null) return null;
                parameterTypes[i] = TypeNode.GetTypeNode(param.ParameterType);
            }

            var paramTypes = new TypeNodeList(parameterTypes);
            var returnType = TypeNode.GetTypeNode(methodInfo.ReturnType);
            var members = tn.GetMembersNamed(Identifier.For(methodInfo.Name));
            for (int i = 0, m = members == null ? 0 : members.Count; i < m; i++)
            {
                var meth = members[i] as Method;
                if (meth == null) continue;
                if (!meth.ParameterTypesMatch(paramTypes)) continue;
                if (meth.ReturnType != returnType) continue;
                return meth;
            }

            return null;
        }
#endif
#if !NoReflection && !MinimalReader && WHIDBEY && !CodeContracts
    protected System.Reflection.Emit.DynamicMethod dynamicMethod;
    public virtual System.Reflection.Emit.DynamicMethod GetDynamicMethod(){
      return this.GetDynamicMethod(false);
    }
    public virtual System.Reflection.Emit.DynamicMethod GetDynamicMethod(bool skipVisibility)
      //^ requires this.DeclaringType != null && this.DeclaringType.DeclaringModule != null && this.IsNormalized && this.Name != null  && this.ReturnType != null;
      //^ requires (this.CallingConvention & CallingConventionFlags.ArgumentConvention) == CallingConventionFlags.StandardCall;
      //^ requires !this.IsGeneric;
    {
      if (this.dynamicMethod == null){
        if (this.DeclaringType == null || this.DeclaringType.DeclaringModule == null || !this.IsNormalized || this.Name == null || this.ReturnType == null){
          Debug.Assert(false); return null;
        }
        if ((this.CallingConvention & CallingConventionFlags.ArgumentConvention) != CallingConventionFlags.StandardCall || this.IsGeneric){
          Debug.Assert(false); return null;
        }
        string name = this.Name.Name;
        System.Reflection.MethodAttributes attrs = (System.Reflection.MethodAttributes)this.Flags;
        System.Reflection.CallingConventions callConv = System.Reflection.CallingConventions.Standard;
        callConv |=
 (System.Reflection.CallingConventions)(this.CallingConvention & ~CallingConventionFlags.ArgumentConvention);
        System.Type rtype = this.ReturnType.GetRuntimeType();
        System.Type owner = this.DeclaringType.GetRuntimeType();
        if (owner == null) { Debug.Fail(""); return null; }
        System.Reflection.Module module = owner.Module;
        System.Reflection.Emit.DynamicMethod dmeth;
        int numPars = this.Parameters == null ? 0 : this.Parameters.Count;
        System.Type[] paramTypes = new Type[numPars];
        for (int i = 0; i < numPars; i++){
          Parameter par = this.Parameters[i];
          if (par == null || par.Type == null){Debug.Assert(false); return null;}
          paramTypes[i] = par.Type.GetRuntimeType();
        }
        if (this.DeclaringType == this.DeclaringType.DeclaringModule.Types[0])
          dmeth =
 new System.Reflection.Emit.DynamicMethod(name, attrs, callConv, rtype, paramTypes, module, skipVisibility);
        else
          dmeth =
 new System.Reflection.Emit.DynamicMethod(name, attrs, callConv, rtype, paramTypes, owner, skipVisibility);
        dmeth.InitLocals = true;
        ReGenerator reGenerator = new ReGenerator(dmeth.GetILGenerator());
        reGenerator.VisitMethod(this);
      }
      return this.dynamicMethod;
    }
#endif
#if !NoReflection
        protected Reflection.MethodInfo methodInfo;
        public virtual Reflection.MethodInfo GetMethodInfo()
        {
            if (methodInfo == null)
            {
                if (DeclaringType == null) return null;
#if WHIDBEY
                if (IsGeneric && Template != null)
                    try
                    {
                        var templateInfo = Template.GetMethodInfo();
                        if (templateInfo == null) return null;
                        var args = TemplateArguments;
                        var arguments = new Type[args.Count];
                        for (var i = 0; i < args.Count; i++) arguments[i] = args[i].GetRuntimeType();
                        return templateInfo.MakeGenericMethod(arguments);
                    }
                    catch
                    {
                        //TODO: log error
                        return null;
                    }
#endif
                var t = DeclaringType.GetRuntimeType();
                if (t == null) return null;
                var retType = typeof(object);
                if (!IsGeneric)
                {
                    //Can't do this for generic methods since it may involve a method type parameter
                    retType = ReturnType.GetRuntimeType();
                    if (retType == null) return null;
                }

                var pars = Parameters;
                var n = pars == null ? 0 : pars.Count;
                var types = new Type[n];
                for (var i = 0; i < n; i++)
                {
                    var p = pars[i];
                    if (p == null || p.Type == null) return null;
                    Type pt;
                    if (IsGeneric)
                        pt = types[i] =
                            typeof(object); //Have to cheat here since the type might involve a type parameter of the method and getting the runtime type for that is a problem
                    //unless we already have the method info in hand
                    else
                        pt = types[i] = p.Type.GetRuntimeType();
                    if (pt == null) return null;
                }

                var members = t.GetMember(Name.ToString(), MemberTypes.Method,
                    BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
                    BindingFlags.NonPublic);
                foreach (Reflection.MethodInfo meth in members)
                {
                    if (meth == null) continue;
                    if (meth.IsStatic != IsStatic) continue;
                    if (meth.ReturnType != retType) continue;
#if WHIDBEY
                    if (meth.IsGenericMethodDefinition)
                    {
                        var templateParams = TemplateParameters;
                        var genericArgs = meth.GetGenericArguments();
                        if (templateParams == null || genericArgs == null ||
                            templateParameters.Count != genericArgs.Length) goto tryNext;
                        for (int i = 0, m = genericArgs.Length; i < m; i++)
                        {
                            var t1 = templateParameters[i];
                            var t2 = genericArgs[i];
                            if (t1 == null || t2 == null || t1.Name == null || t1.Name.Name != t2.Name) goto tryNext;
                        }
                    }
#endif
                    var parameters = meth.GetParameters();
                    var parCount = parameters == null ? 0 : parameters.Length;
                    if (parCount != n) continue;
                    for (var i = 0; i < n; i++)
                    {
                        //^ assert parameters != null;
                        var par = parameters[i];
                        if (par == null) goto tryNext;
                        if (IsGeneric)
                        {
                            //We don't have the runtime type for the parameter, so just check that the name is the same
                            var p = pars[i];
                            if (par.Name != p.Name.Name) goto tryNext;
                        }
                        else
                        {
                            if (par.ParameterType != types[i]) goto tryNext;
                        }
                    }

                    return methodInfo = meth;
                    tryNext: ;
                }
            }

            return methodInfo;
        }
#endif
#if !MinimalReader
        protected TypeNode[] parameterTypes;
        public virtual TypeNode[] /*!*/ GetParameterTypes()
        {
            if (parameterTypes != null) return parameterTypes;
            var pars = Parameters;
            var n = pars == null ? 0 : pars.Count;
            var types = parameterTypes = new TypeNode[n];
            for (var i = 0; i < n; i++)
            {
                var p = pars[i];
                if (p == null) continue;
                types[i] = p.Type;
            }

            return types;
        }
#endif
        public virtual bool ParametersMatch(ParameterList parameters)
        {
            var pars = Parameters;
            var n = pars == null ? 0 : pars.Count;
            var m = parameters == null ? 0 : parameters.Count;
            if (n != m) return false;
            if (parameters == null) return true;
            for (var i = 0; i < n; i++)
            {
                var par1 = pars[i];
                var par2 = parameters[i];
                if (par1 == null || par2 == null) return false;
                if (par1.Type != par2.Type) return false;
            }

            return true;
        }
#if !MinimalReader
        public virtual bool ParametersMatchExceptLast(ParameterList parameters)
        {
            var pars = Parameters;
            var n = pars == null ? 0 : pars.Count;
            var m = parameters == null ? 0 : parameters.Count;
            if (n != m) return false;
            if (parameters == null) return true;
            for (var i = 0; i < n - 1; i++)
            {
                var par1 = pars[i];
                var par2 = parameters[i];
                if (par1 == null || par2 == null) return false;
                if (par1.Type != par2.Type) return false;
            }

            return true;
        }
#endif
        public virtual bool ParametersMatchStructurally(ParameterList parameters)
        {
            var pars = Parameters;
            var n = pars == null ? 0 : pars.Count;
            var m = parameters == null ? 0 : parameters.Count;
            if (n != m) return false;
            if (parameters == null) return true;
            for (var i = 0; i < n; i++)
            {
                var par1 = pars[i];
                var par2 = parameters[i];
                if (par1 == null || par2 == null) return false;
                if (par1.Type == null || par2.Type == null) return false;
                if (par1.Type != par2.Type && !par1.Type.IsStructurallyEquivalentTo(par2.Type)) return false;
            }

            return true;
        }
#if !MinimalReader
        public virtual bool ParametersMatchStructurallyIncludingOutFlag(ParameterList parameters)
        {
            return ParametersMatchStructurallyIncludingOutFlag(parameters, false);
        }

        public virtual bool ParametersMatchStructurallyIncludingOutFlag(ParameterList parameters, bool allowCoVariance)
        {
            var pars = Parameters;
            var n = pars == null ? 0 : pars.Count;
            var m = parameters == null ? 0 : parameters.Count;
            if (n != m) return false;
            if (parameters == null) return true;
            for (var i = 0; i < n; i++)
            {
                var par1 = pars[i];
                var par2 = parameters[i];
                if (par1 == null || par2 == null) return false;
                if (par1.Type == null || par2.Type == null) return false;
                if ((par1.Flags & ParameterFlags.Out) != (par2.Flags & ParameterFlags.Out)) return false;
                if (par1.Type != par2.Type && !par1.Type.IsStructurallyEquivalentTo(par2.Type))
                {
                    if (allowCoVariance && !par2.Type.IsValueType) return par2.Type.IsAssignableTo(par1.Type);
                    return false;
                }
            }

            return true;
        }

        public virtual bool ParametersMatchStructurallyExceptLast(ParameterList parameters)
        {
            var pars = Parameters;
            var n = pars == null ? 0 : pars.Count;
            var m = parameters == null ? 0 : parameters.Count;
            if (n != m) return false;
            if (parameters == null) return true;
            for (var i = 0; i < n - 1; i++)
            {
                var par1 = pars[i];
                var par2 = parameters[i];
                if (par1 == null || par2 == null) return false;
                if (par1.Type == null || par2.Type == null) return false;
                if (par1.Type != par2.Type && !par1.Type.IsStructurallyEquivalentTo(par2.Type)) return false;
            }

            return true;
        }

        public virtual bool ParametersMatchIncludingOutFlag(ParameterList parameters)
        {
            var pars = Parameters;
            var n = pars == null ? 0 : pars.Count;
            var m = parameters == null ? 0 : parameters.Count;
            if (n != m) return false;
            if (parameters == null) return true;
            for (var i = 0; i < n; i++)
            {
                var par1 = pars[i];
                var par2 = parameters[i];
                if (par1.Type != par2.Type) return false;
                if ((par1.Flags & ParameterFlags.Out) != (par2.Flags & ParameterFlags.Out)) return false;
            }

            return true;
        }
#endif
        public virtual bool ParameterTypesMatch(TypeNodeList argumentTypes)
        {
            var n = Parameters == null ? 0 : Parameters.Count;
            var m = argumentTypes == null ? 0 : argumentTypes.Count;
            if (n != m) return false;
            if (argumentTypes == null) return true;
            for (var i = 0; i < n; i++)
            {
                var par = Parameters[i];
                if (par == null) return false;
                var argType = argumentTypes[i];
                if (par.Type != argType)
                {
                    var pType = TypeNode.StripModifiers(par.Type);
                    argType = TypeNode.StripModifiers(argType);
                    if (pType != argType) return false;
                }
            }

            return true;
        }

        public virtual bool ParameterTypesMatchStructurally(TypeNodeList argumentTypes)
        {
            var n = Parameters == null ? 0 : Parameters.Count;
            var m = argumentTypes == null ? 0 : argumentTypes.Count;
            if (n != m) return false;
            if (argumentTypes == null) return true;
            for (var i = 0; i < n; i++)
            {
                var par = Parameters[i];
                var argType = argumentTypes[i];
                if (par.Type != argType)
                {
                    var pType = TypeNode.StripModifiers(par.Type);
                    argType = TypeNode.StripModifiers(argType);
                    if (pType == null || !pType.IsStructurallyEquivalentTo(argType)) return false;
                }
            }

            return true;
        }

        public virtual bool TemplateParametersMatch(TypeNodeList templateParameters)
        {
            var locPars = TemplateParameters;
            if (locPars == null) return templateParameters == null || templateParameters.Count == 0;
            if (templateParameters == null) return false;
            var n = locPars.Count;
            if (n != templateParameters.Count) return false;
            for (var i = 0; i < n; i++)
            {
                var tp1 = locPars[i];
                var tp2 = templateParameters[i];
                if (tp1 == null || tp2 == null) return false;
                if (tp1 != tp2 && !tp1.IsStructurallyEquivalentTo(tp2)) return false;
            }

            return true;
        }
#if !ROTOR
        internal TrivialHashtable contextForOffset;
        internal void RecordSequencePoints(ISymUnmanagedMethod methodInfo,
            Dictionary<IntPtr, UnmanagedDocument> documentCache)
        {
            if (methodInfo == null || contextForOffset != null) return;
            var count = methodInfo.GetSequencePointCount();
            contextForOffset = new TrivialHashtable((int)count);
            var docPtrs = new IntPtr[count];
            var startLines = new uint[count];
            var startCols = new uint[count];
            var endLines = new uint[count];
            var endCols = new uint[count];
            var offsets = new uint[count];
            uint numPoints;
            methodInfo.GetSequencePoints(count, out numPoints, offsets, docPtrs, startLines, startCols, endLines,
                endCols);
            Debug.Assert(count == numPoints);
            for (var i = 0; i < count; i++)
            {
                //The magic hex constant below works around weird data reported from GetSequencePoints.
                //The constant comes from ILDASM's source code, which performs essentially the same test.
                const uint MagicHidden = 0xFEEFEE;
                if (startLines[i] >= MagicHidden || endLines[i] >= MagicHidden)
                {
                    var doc = UnmanagedDocument.For(documentCache, docPtrs[i]);
                    contextForOffset[(int)offsets[i] + 1] =
#if !FxCop
                        new SourceContext(doc, -1, -1);
#else
            new SourceContext(null, MagicHidden, 0, 0, 0);
#endif
                }
                else
                {
                    var doc = UnmanagedDocument.For(documentCache, docPtrs[i]);
                    contextForOffset[(int)offsets[i] + 1] =
#if !FxCop
                        new SourceContext(doc, doc.GetOffset(startLines[i], startCols[i]),
                            doc.GetOffset(endLines[i], endCols[i]));
#else
            new SourceContext(doc.Name, startLines[i], endLines[i], startCols[i], endCols[i]);
#endif
                }
            }

            for (var i = 0; i < count; i++)
                Marshal.Release(docPtrs[i]);
        }
#endif
        private static readonly Method NotSpecified = new Method();
        private Method template;

        /// <summary>
        ///     The (generic) method template from which this method was instantiated. Null if this is not a (generic) method
        ///     template instance.
        /// </summary>
        public Method Template
        {
            get
            {
                var result = template;
#if ExtendedRuntime
        if (result == null){
          AttributeList attributes = this.Attributes;
          lock(this){
            if (this.template != null) return this.template;
            for (int i = 0, n = attributes == null ? 0 : attributes.Count; i < n; i++){
              AttributeNode attr = attributes[i];
              if (attr == null) continue;
              MemberBinding mb = attr.Constructor as MemberBinding;
              if (mb == null || mb.BoundMember == null || mb.BoundMember.DeclaringType != SystemTypes.TemplateInstanceAttribute) continue;
              ExpressionList exprs = attr.Expressions;
              if (exprs == null || exprs.Count != 2) continue;
              Literal lit = exprs[0] as Literal;
              if (lit == null) continue;
              TypeNode templ = lit.Value as TypeNode;
              if (templ != null){
                lit = exprs[1] as Literal;
                if (lit == null) continue;
                object[] types = lit.Value as object[];
                if (types == null) continue;
                int m = types == null ? 0 : types.Length;
                TypeNodeList templateArguments = new TypeNodeList(m);
                for (int j = 0; j < m; j++){
                  //^ assert types != null;
                  TypeNode t = types[j] as TypeNode;
                  if (t == null) continue;
                  templateArguments.Add(t);
                }
                this.TemplateArguments = templateArguments;
                MemberList members = templ.GetMembersNamed(this.Name);
                if (members != null)
                  for (int j = 0, k = members.Count; j < k; j++){
                    Method meth = members[j] as Method;
                    if (meth == null) continue;
                    if (meth.ParametersMatch(this.Parameters)){
                      this.template = result = meth; break;
                    }
                  }
              }
            }
            if (result == null)
              this.template = Method.NotSpecified;
          }
        }else
#endif
                if (result == NotSpecified)
                    return null;
                return result;
            }
            set { template = value; }
        }

        /// <summary>
        ///     The arguments used when this (generic) method template instance was instantiated.
        /// </summary>
        public TypeNodeList TemplateArguments { get; set; }

        internal TypeNodeList templateParameters;
#if CodeContracts
        public ExtraPDBInfo ExtraDebugInfo;
        public bool IsAsync;
        public int? MoveNextStartState;
#endif
        public virtual TypeNodeList TemplateParameters
        {
            get
            {
#if CodeContracts
                CC.Contract.Ensures(CC.Contract.Result<TypeNodeList>() == null
                                    || CC.Contract.ForAll(0, CC.Contract.Result<TypeNodeList>().Count,
                                        i => ((ITypeParameter)CC.Contract.Result<TypeNodeList>()[i])
                                            .ParameterListIndex == i));
#endif

                var result = templateParameters;
#if ExtendedRuntime
        if (result == null && this.Template == null){
          this.TemplateParameters = result = new TypeNodeList();
          AttributeList attributes = this.Attributes;
          for (int i = 0, n = attributes == null ? 0 : attributes.Count; i < n; i++){
            AttributeNode attr = attributes[i];
            if (attr == null) continue;
            MemberBinding mb = attr.Constructor as MemberBinding;
            if (mb == null || mb.BoundMember == null || mb.BoundMember.DeclaringType != SystemTypes.TemplateAttribute) continue;
            ExpressionList exprs = attr.Expressions;
            if (exprs == null || exprs.Count != 1) continue;
            Literal lit = exprs[0] as Literal;
            if (lit == null) continue;
            object[] types = lit.Value as object[];
            if (types == null) continue;
            for (int j = 0, m = types == null ? 0 : types.Length; j < m; j++){
              TypeNode t = types[j] as TypeNode;
              if (t == null) continue;
              if (t.NodeType == NodeType.TypeParameter || t.NodeType == NodeType.ClassParameter)
                result.Add(t);
            }
            attributes[i] = null;
          }
        }
        if (result == null || result.Count == 0) return null;
#endif
                return result;
            }
            [SuppressMessage("Microsoft.Contracts",
                "RequiresAtCall-value == null || CC.Contract.ForAll(0, value.Count, i => ((ITypeParameter)value[i]).ParameterListIndex == i)")]
            set
            {
#if CodeContracts
                CC.Contract.Requires(value == null || CC.Contract.ForAll(0, value.Count,
                    i => ((ITypeParameter)value[i]).ParameterListIndex == i));
#endif
                templateParameters = value;
            }
        }

        public virtual Method /*!*/ GetTemplateInstance(TypeNode referringType, params TypeNode[] typeArguments)
        {
            return GetTemplateInstance(referringType, new TypeNodeList(typeArguments));
        }

        public virtual Method /*!*/ GetTemplateInstance(TypeNode referringType, TypeNodeList typeArguments)
        {
            if (!IsGeneric && (referringType == null || DeclaringType == null))
            {
                Debug.Assert(false);
                return this;
            }

            if (IsGeneric) referringType = DeclaringType;
            if (referringType != DeclaringType && referringType.DeclaringModule == DeclaringType.DeclaringModule)
                return GetTemplateInstance(DeclaringType, typeArguments);
            if (referringType.structurallyEquivalentMethod == null)
                referringType.structurallyEquivalentMethod = new TrivialHashtableUsingWeakReferences();
            var module = referringType.DeclaringModule;
            if (module == null) return this;
            var n = typeArguments == null ? 0 : typeArguments.Count;
            if (n == 0 || typeArguments == null) return this;
            var uniqueMangledName = TypeNode.GetUniqueMangledTemplateInstanceName(UniqueKey, typeArguments);

            lock (this)
            {
                var m = (Method)referringType.structurallyEquivalentMethod[uniqueMangledName.UniqueIdKey];
                if (m != null)
                    return m;
                var sb = new StringBuilder(Name.ToString());
                sb.Append('<');
                for (var i = 0; i < n; i++)
                {
                    var ta = typeArguments[i];
                    if (ta == null) continue;
                    sb.Append(ta.FullName);
                    if (i < n - 1) sb.Append(',');
                }

                sb.Append('>');
                var mangledName = Identifier.For(sb.ToString());
                var duplicator = new Duplicator(referringType.DeclaringModule, referringType);
                duplicator.RecordOriginalAsTemplate = true;
                duplicator.SkipBodies = true;
                var result = duplicator.VisitMethodInternal(this);
                //^ assume result != null;
                result.Attributes = Attributes; //These do not get specialized, but may need to get normalized
                result.Name = mangledName;
                result.fullName = null;
                result.template = this;
                result.TemplateArguments = typeArguments;
                var templateParameters = result.TemplateParameters;
                result.TemplateParameters = null;
#if !MinimalReader
                result.IsNormalized = true;
#endif
                if (!IsGeneric)
                {
                    var pars = Parameters;
                    var rpars = result.Parameters;
                    if (pars != null && rpars != null && rpars.Count >= pars.Count)
                        for (int i = 0, count = pars.Count; i < count; i++)
                        {
                            var p = pars[i];
                            var rp = rpars[i];
                            if (p == null || rp == null) continue;
                            rp.Attributes = p.Attributes; //These do not get specialized, but may need to get normalized
                        }
                }

                if (!IsGeneric && !result.IsStatic && DeclaringType != referringType)
                {
                    result.Flags &= ~(MethodFlags.Virtual | MethodFlags.NewSlot);
                    result.Flags |= MethodFlags.Static;
                    result.CallingConvention &= ~CallingConventionFlags.HasThis;
                    result.CallingConvention |= CallingConventionFlags.ExplicitThis;
                    var pars = result.Parameters;
                    if (pars == null) result.Parameters = pars = new ParameterList(1);
                    var thisPar = new Parameter(StandardIds.This, DeclaringType);
                    pars.Add(thisPar);
                    for (var i = pars.Count - 1; i > 0; i--)
                        pars[i] = pars[i - 1];
                    pars[0] = thisPar;
                }

                referringType.structurallyEquivalentMethod[uniqueMangledName.UniqueIdKey] = result;

                var specializer = new Specializer(module, templateParameters, typeArguments);
                specializer.VisitMethod(result);
                if (IsGeneric)
                {
                    result.DeclaringType = DeclaringType;
                    return result;
                }

                if (IsAbstract) return result;
                referringType.Members.Add(result);
                return result;
            }
        }

        private static bool TypeListsAreEquivalent(TypeNodeList list1, TypeNodeList list2)
        {
            if (list1 == null || list2 == null) return list1 == list2;
            var n = list1.Count;
            if (n != list2.Count) return false;
            for (var i = 0; i < n; i++)
                if (list1[i] != list2[i])
                    return false;
            return true;
        }
#if !MinimalReader && !CodeContracts
    /// <summary>
    /// Returns the local associated with the given field, allocating a new local if necessary.
    /// </summary>
    public virtual Local/*!*/ GetLocalForField(Field/*!*/ f){
      if (this.Locals == null)
      {
        this.Locals = new TrivialHashtable();
      }
      Local loc = (Local)this.Locals[f.UniqueKey];
      if (loc == null){
        this.Locals[f.UniqueKey] = loc = new Local(f.Name, f.Type);
        loc.SourceContext = f.Name.SourceContext;
      }
      return loc;
    }
#endif
        //TODO: Also need to add a method for allocating locals
        public Method CreateExplicitImplementation(TypeNode implementingType, ParameterList parameters,
            StatementList body)
        {
            var m = new Method(implementingType, null, Name, parameters, ReturnType, new Block(body));
            m.CallingConvention = CallingConventionFlags.HasThis;
            m.Flags = MethodFlags.Public | MethodFlags.HideBySig | MethodFlags.Virtual | MethodFlags.NewSlot |
                      MethodFlags.Final;
            m.ImplementedInterfaceMethods = new MethodList(this);
            //m.ImplementedTypes = new TypeNodeList(this.DeclaringType);
            return m;
        }

        public virtual bool TypeParameterCountsMatch(Method meth2)
        {
            if (meth2 == null) return false;
            var n = TemplateParameters == null ? 0 : TemplateParameters.Count;
            var m = meth2.TemplateParameters == null ? 0 : meth2.TemplateParameters.Count;
            return n == m;
        }

        public override string ToString()
        {
            return DeclaringType.GetFullUnmangledNameWithTypeParameters() + "." + Name;
        }
#if !MinimalReader && !CodeContracts
    public bool GetIsCompilerGenerated() {
      InstanceInitializer ii = this as InstanceInitializer;
      return this.HasCompilerGeneratedSignature || (ii != null && ii.IsCompilerGenerated);
    }
#endif
#if FxCop
    internal override void GetName(MemberFormat options, StringBuilder name)
    {
      base.GetName(options, name);
      AppendTypeParameters(options, name);
      AppendParametersAndReturnType(options, this.Parameters, '(', ')', this.ReturnType, name);
    }
    private void AppendTypeParameters(MemberFormat options, StringBuilder name)
    {
      if (options.ShowGenericMethodTypeParameterNames == false
        || this.templateParameters == null
        || this.templateParameters.Count == 0)
        return;

        name.Append('<');
        TypeNodeList templateParameters = this.TemplateParameters;
        for (int i = 0; i < templateParameters.Count; i++)
        {
          TypeNode templateParameter = templateParameters[i];
          if (i != 0)
          {
            name.Append(',');
            if (options.InsertSpacesBetweenMethodTypeParameters)
              name.Append(' ');
          }
          name.Append(templateParameter.Name.Name);
        }
        name.Append('>');
    }

    internal static void AppendParametersAndReturnType(MemberFormat options, ParameterCollection parameters, char parametersPrefix, char parametersSuffix, TypeNode returnType, StringBuilder name)
    {
      AppendParameters(options.Parameters, parameters, parametersPrefix, parametersSuffix, name);
      AppendReturnType(options.ReturnType, returnType, name);     
    }

    internal static void AppendParameters(ParameterFormat options, ParameterCollection parameters, char prefix, char suffix, StringBuilder name)
    {
        if (parameters == null)
            return;

        if (options.TypeName == TypeNameFormat.None && options.ShowParameterNames == false)
          return;

        name.Append(prefix);
        for (int i = 0; i < parameters.Count; ++i)
        {
          Parameter parameter = parameters[i];
          if (i > 0)
          {
            name.Append(',');
            if (options.InsertSpacesBetweenParameters)
              name.Append(' ');
          }
          if (options.TypeName != TypeNameFormat.None)
          {
            parameter.Type.GetName(options, name);
            if (options.ShowParameterNames) name.Append(' ');
          }
          if (options.ShowParameterNames)
            name.Append(parameter.Name.Name);
        }
        name.Append(suffix);
    }

    internal static void AppendReturnType(TypeFormat options, TypeNode returnType, StringBuilder name)
    {
      if (options.TypeName == TypeNameFormat.None)
        return;

      name.Append(':');
      returnType.GetName(options, name);
    }
#endif
    }
#if !MinimalReader
    public class ProxyMethod : Method
    {
        public Method ProxyFor;

        public ProxyMethod(TypeNode declaringType, AttributeList attributes, Identifier name, ParameterList parameters,
            TypeNode returnType, Block body)
            : base(declaringType, attributes, name, parameters, returnType, body)
        {
        }
    }
#endif
    public class InstanceInitializer : Method
    {
#if !MinimalReader
        /// <summary>
        ///     True if this constructor calls a constructor declared in the same class, as opposed to the base class.
        /// </summary>
        public bool IsDeferringConstructor;

        /// <summary>
        ///     When the source uses the C# compatibility mode, base calls cannot be put after non-null
        ///     field initialization, but must be put before the body. But the user can specify where
        ///     the base ctor call should be performed by using "base;" as a marker. During parsing
        ///     this flag is set so the right code transformations can be performed at code generation.
        /// </summary>
        public bool ContainsBaseMarkerBecauseOfNonNullFields;

        public Block BaseOrDefferingCallBlock;
        public bool IsCompilerGenerated = false;
#endif
        public InstanceInitializer()
        {
            NodeType = NodeType.InstanceInitializer;
            CallingConvention = CallingConventionFlags.HasThis;
            Flags = MethodFlags.SpecialName | MethodFlags.RTSpecialName;
            Name = StandardIds.Ctor;
            ReturnType = CoreSystemTypes.Void;
        }

        public InstanceInitializer(MethodBodyProvider provider, object handle)
            : base(provider, handle)
        {
            NodeType = NodeType.InstanceInitializer;
        }
#if ILOFFSETS
        public InstanceInitializer(MethodBodyProvider provider, object handle, int methodToken)
            : base(provider, handle, methodToken)
        {
            NodeType = NodeType.InstanceInitializer;
        }
#endif
#if !MinimalReader
        public InstanceInitializer(TypeNode declaringType, AttributeList attributes, ParameterList parameters,
            Block body)
            : this(declaringType, attributes, parameters, body, CoreSystemTypes.Void)
        {
        }

        public InstanceInitializer(TypeNode declaringType, AttributeList attributes, ParameterList parameters,
            Block body, TypeNode returnType)
            : base(declaringType, attributes, StandardIds.Ctor, parameters, null, body)
        {
            NodeType = NodeType.InstanceInitializer;
            CallingConvention = CallingConventionFlags.HasThis;
            Flags = MethodFlags.SpecialName | MethodFlags.RTSpecialName;
            Name = StandardIds.Ctor;
            ReturnType = returnType;
        }
#endif
#if !NoReflection
        protected ConstructorInfo constructorInfo;
        public virtual ConstructorInfo GetConstructorInfo()
        {
            if (constructorInfo == null)
            {
                if (DeclaringType == null) return null;
                var t = DeclaringType.GetRuntimeType();
                if (t == null) return null;
                var pars = Parameters;
                var n = pars == null ? 0 : pars.Count;
                var types = new Type[n];
                for (var i = 0; i < n; i++)
                {
                    var p = pars[i];
                    if (p == null || p.Type == null) return null;
                    var pt = types[i] = p.Type.GetRuntimeType();
                    if (pt == null) return null;
                }

                var members = t.GetMember(Name.ToString(), MemberTypes.Constructor,
                    BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (ConstructorInfo cons in members)
                {
                    if (cons == null) continue;
                    var parameters = cons.GetParameters();
                    if (parameters != null)
                    {
                        if (parameters.Length != n) continue;
                        for (var i = 0; i < n; i++)
                        {
                            var par = parameters[i];
                            if (par == null || par.ParameterType != types[i]) goto tryNext;
                        }
                    }

                    return constructorInfo = cons;
                    tryNext: ;
                }
            }

            return constructorInfo;
        }
#endif
#if !NoReflection
        public override Reflection.MethodInfo GetMethodInfo()
        {
            return null;
        }

        public virtual object Invoke(params object[] arguments)
        {
            var constr = GetConstructorInfo();
            if (constr == null) return null;
            return constr.Invoke(arguments);
        }

        public virtual Literal Invoke(params Literal[] arguments)
        {
            var n = arguments == null ? 0 : arguments.Length;
            var args = n == 0 ? null : new object[n];
            if (args != null && arguments != null)
                for (var i = 0; i < n; i++)
                {
                    var lit = arguments[i];
                    args[i] = lit == null ? null : lit.Value;
                }

            return new Literal(Invoke(args));
        }
#endif
        //initializers never override a base class initializer
        public override bool OverridesBaseClassMember
        {
            get { return false; }
            set { }
        }

        public override Member OverriddenMember
        {
            get { return null; }
            set { }
        }

        public override Method OverriddenMethod
        {
            get { return null; }
            set { }
        }

        public override string ToString()
        {
            return DeclaringType.GetFullUnmangledNameWithTypeParameters() + "(" + Parameters + ")";
        }
#if !MinimalReader
        public virtual MemberList GetAttributeConstructorNamedParameters()
        {
            var type = DeclaringType;
            if (type == null || !type.IsAssignableTo(SystemTypes.Attribute) || type.Members == null)
                return null;
            var memList = type.Members;
            var n = memList.Count;
            var ml = new MemberList(memList.Count);
            for (var i = 0; i < n; ++i)
            {
                var p = memList[i] as Property;
                if (p != null && p.IsPublic)
                {
                    if (p.Setter != null && p.Getter != null)
                        ml.Add(p);
                    continue;
                }

                var f = memList[i] as Field;
                if (f != null && !f.IsInitOnly && f.IsPublic) ml.Add(f);
            }

            return ml;
        }
#endif
#if FxCop
    internal override void GetName(MemberFormat options, StringBuilder name)
    {
      GetInitializerName(options, this.DeclaringType, this.Parameters, name, StandardIds.Ctor.Name);
    }
    internal static void GetInitializerName(MemberFormat options, TypeNode declaringType, ParameterCollection parameters, StringBuilder name, string methodName)
    {
      if (options.Type.TypeName != TypeNameFormat.None)
      {
        declaringType.GetName(options, name);
        name.Append('.');
      }
      name.Append(methodName);
      AppendParameters(options.Parameters, parameters, '(', ')', name);
    }
#endif
    }

    public class StaticInitializer : Method
    {
        public StaticInitializer()
        {
            NodeType = NodeType.StaticInitializer;
            Flags = MethodFlags.SpecialName | MethodFlags.RTSpecialName | MethodFlags.Static | MethodFlags.HideBySig |
                    MethodFlags.Private;
            Name = StandardIds.CCtor;
            ReturnType = CoreSystemTypes.Void;
        }
#if ILOFFSETS
        public StaticInitializer(MethodBodyProvider provider, object handle, int methodToken)
            : base(provider, handle, methodToken)
        {
            NodeType = NodeType.StaticInitializer;
        }
#endif
        public StaticInitializer(MethodBodyProvider provider, object handle)
            : base(provider, handle)
        {
            NodeType = NodeType.StaticInitializer;
        }
#if !MinimalReader
        public StaticInitializer(TypeNode declaringType, AttributeList attributes, Block body)
            : base(declaringType, attributes, StandardIds.CCtor, null, null, body)
        {
            NodeType = NodeType.StaticInitializer;
            Flags = MethodFlags.SpecialName | MethodFlags.RTSpecialName | MethodFlags.Static | MethodFlags.HideBySig |
                    MethodFlags.Private;
            Name = StandardIds.CCtor;
            ReturnType = CoreSystemTypes.Void;
        }

        public StaticInitializer(TypeNode declaringType, AttributeList attributes, Block body,
            TypeNode voidTypeExpression)
            : base(declaringType, attributes, StandardIds.CCtor, null, null, body)
        {
            NodeType = NodeType.StaticInitializer;
            Flags = MethodFlags.SpecialName | MethodFlags.RTSpecialName | MethodFlags.Static | MethodFlags.HideBySig |
                    MethodFlags.Private;
            Name = StandardIds.CCtor;
            ReturnType = voidTypeExpression;
        }
#endif
#if !NoReflection
        protected ConstructorInfo constructorInfo;

        public virtual ConstructorInfo GetConstructorInfo()
        {
            if (constructorInfo == null)
            {
                if (DeclaringType == null) return null;
                var t = DeclaringType.GetRuntimeType();
                if (t == null) return null;
                var pars = Parameters;
                var n = pars == null ? 0 : pars.Count;
                var types = new Type[n];
                for (var i = 0; i < n; i++)
                {
                    var p = pars[i];
                    if (p == null || p.Type == null) return null;
                    var pt = types[i] = p.Type.GetRuntimeType();
                    if (pt == null) return null;
                }

                var members = t.GetMember(Name.ToString(), MemberTypes.Constructor,
                    BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (ConstructorInfo cons in members)
                {
                    if (cons == null) continue;
                    var parameters = cons.GetParameters();
                    var numPars = parameters == null ? 0 : parameters.Length;
                    if (numPars != n) continue;
                    if (parameters != null)
                        for (var i = 0; i < n; i++)
                        {
                            var par = parameters[i];
                            if (par == null || par.ParameterType != types[i]) goto tryNext;
                        }

                    return constructorInfo = cons;
                    tryNext: ;
                }
            }

            return constructorInfo;
        }

        public override Reflection.MethodInfo GetMethodInfo()
        {
            return null;
        }
#endif
        //initializers never override a base class initializer
        public override bool OverridesBaseClassMember
        {
            get { return false; }
            set { }
        }

        public override Member OverriddenMember
        {
            get { return null; }
            set { }
        }

        public override Method OverriddenMethod
        {
            get { return null; }
            set { }
        }
#if FxCop
    internal override void GetName(MemberFormat options, StringBuilder name)
    {
      InstanceInitializer.GetInitializerName(options, this.DeclaringType, this.Parameters, name, StandardIds.CCtor.Name);
    }
#endif
    }
#if !MinimalReader
    public class FieldInitializerBlock : Block
    {
        public bool IsStatic;
        public TypeNode Type;

        public FieldInitializerBlock()
        {
            NodeType = NodeType.FieldInitializerBlock;
        }

        public FieldInitializerBlock(TypeNode type, bool isStatic)
        {
            NodeType = NodeType.FieldInitializerBlock;
            Type = type;
            IsStatic = isStatic;
        }
    }
#endif
#if !MinimalReader
    public class ParameterField : Field
    {
        protected Parameter parameter;

        public ParameterField()
        {
        }

        public ParameterField(TypeNode declaringType, AttributeList attributes, FieldFlags flags, Identifier name,
            TypeNode Type, Literal defaultValue)
            : base(declaringType, attributes, flags, name, Type, defaultValue)
        {
        }

        public virtual Parameter Parameter
        {
            get { return parameter; }
            set { parameter = value; }
        }
    }
#endif
    /// <summary>
    ///     Used to transmit info from il reader to dummy member generator
    /// </summary>
    internal class FieldInfo
    {
        public bool IsStatic;
    }

    public class Field : Member
    {
#if !MinimalReader
        /// <summary>Provides a value that is assigned to the field upon initialization.</summary>
        public Expression Initializer;

        public TypeNode TypeExpression;
        public bool HasOutOfBandContract;
        public InterfaceList ImplementedInterfaces;

        public InterfaceList ImplementedInterfaceExpressions;

        // if this is the backing field for some event, then ForEvent is that event
        public Event ForEvent;
        public bool
            IsModelfield = false; //set to true if this field serves as the representation of a modelfield in a class
#endif
        public Field()
            : base(NodeType.Field)
        {
        }

        public Field(Identifier name)
            : base(NodeType.Field)
        {
            Name = name;
        }

        public Field(TypeNode declaringType, AttributeList attributes, FieldFlags flags, Identifier name,
            TypeNode type, Literal defaultValue)
            : base(declaringType, attributes, name, NodeType.Field)
        {
            DefaultValue = defaultValue;
            Flags = flags;
            Type = type;
        }

        /// <summary>The compile-time value to be substituted for references to this field if it is a literal.</summary>
        public Literal DefaultValue
        {
            //TODO: rename this to LiteralValue
            get;
            set;
        }

        public FieldFlags Flags { get; set; }

        public int Offset { get; set; }

        /// <summary>True if the field may not be cached. Used for sharing data between multiple threads.</summary>
        public bool IsVolatile { get; set; }

        /// <summary>The type of values that may be stored in the field.</summary>
        public TypeNode Type { get; set; }

        public MarshallingInformation MarshallingInformation { get; set; }

        public byte[] InitialData { get; set; }

        internal PESection section;

        public PESection Section
        {
            get { return section; }
            set { section = value; }
        }

        protected string fullName;
        public override string /*!*/ FullName
        {
            get
            {
                var result = fullName;
                if (result == null)
                    fullName = result = DeclaringType.FullName + "." + (Name == null ? "" : Name.ToString());
                return result;
            }
        }
#if !NoXml
        protected override Identifier GetDocumentationId()
        {
            if (DeclaringType == null) return Identifier.Empty;
            if (Name == null) return Identifier.Empty;
            var sb = new StringBuilder(DeclaringType.DocumentationId.ToString());
            sb[0] = 'F';
            sb.Append(".");
            sb.Append(Name.Name);
            return Identifier.For(sb.ToString());
        }
#endif
#if !NoReflection
        public static Field GetField(Reflection.FieldInfo fieldInfo)
        {
            if (fieldInfo == null) return null;
            var tn = TypeNode.GetTypeNode(fieldInfo.DeclaringType);
            if (tn == null) return null;
            return tn.GetField(Identifier.For(fieldInfo.Name));
        }
#endif
#if !NoReflection
        protected Reflection.FieldInfo fieldInfo;
        public virtual Reflection.FieldInfo GetFieldInfo()
        {
            if (fieldInfo == null)
            {
                var tn = DeclaringType;
                if (tn == null) return null;
                var t = tn.GetRuntimeType();
                if (t == null) return null;
                var flags = BindingFlags.DeclaredOnly;
                if (IsPublic) flags |= BindingFlags.Public;
                else flags |= BindingFlags.NonPublic;
                if (IsStatic) flags |= BindingFlags.Static;
                else flags |= BindingFlags.Instance;
                fieldInfo = t.GetField(Name.ToString(), flags);
            }

            return fieldInfo;
        }
#endif
        /// <summary>True if all references to the field are replaced with a value that is determined at compile-time.</summary>
        public virtual bool IsLiteral => (Flags & FieldFlags.Literal) != 0;

        public override bool IsAssembly => (Flags & FieldFlags.FieldAccessMask) == FieldFlags.Assembly;

        public override bool IsCompilerControlled =>
            (Flags & FieldFlags.FieldAccessMask) == FieldFlags.CompilerControlled;

        public override bool IsFamily => (Flags & FieldFlags.FieldAccessMask) == FieldFlags.Family;

        public override bool IsFamilyAndAssembly => (Flags & FieldFlags.FieldAccessMask) == FieldFlags.FamANDAssem;

        public override bool IsFamilyOrAssembly => (Flags & FieldFlags.FieldAccessMask) == FieldFlags.FamORAssem;

        /// <summary>True if the field may only be assigned to inside the constructor.</summary>
        public virtual bool IsInitOnly => (Flags & FieldFlags.InitOnly) != 0;

        public override bool IsPrivate => (Flags & FieldFlags.FieldAccessMask) == FieldFlags.Private;

        public override bool IsPublic => (Flags & FieldFlags.FieldAccessMask) == FieldFlags.Public;
        public override bool IsSpecialName => (Flags & FieldFlags.SpecialName) != 0;

        public override bool IsStatic => (Flags & FieldFlags.Static) != 0;
        public override bool IsVisibleOutsideAssembly
        {
            get
            {
                if (DeclaringType != null && !DeclaringType.IsVisibleOutsideAssembly) return false;
                switch (Flags & FieldFlags.FieldAccessMask)
                {
                    case FieldFlags.Public:
                        return true;
                    case FieldFlags.Family:
                    case FieldFlags.FamORAssem:
                        return DeclaringType != null && !DeclaringType.IsSealed;
                    default:
                        return false;
                }
            }
        }
#if !NoReflection
        public virtual object GetValue(object targetObject)
        {
            var fieldInfo = GetFieldInfo();
            if (fieldInfo == null) return null;
            return fieldInfo.GetValue(targetObject);
        }

        public virtual Literal GetValue(Literal /*!*/ targetObject)
        {
            return new Literal(GetValue(targetObject.Value));
        }

        public virtual void SetValue(object targetObject, object value)
        {
            var fieldInfo = GetFieldInfo();
            if (fieldInfo == null) return;
            fieldInfo.SetValue(targetObject, value);
        }

        public virtual void SetValue(Literal /*!*/ targetObject, Literal /*!*/ value)
        {
            SetValue(targetObject.Value, value.Value);
        }
#endif
#if ExtendedRuntime
    ReferenceFieldSemantics referenceSemantics;
    public ReferenceFieldSemantics ReferenceSemantics{
      get{
        if (this.referenceSemantics == ReferenceFieldSemantics.NotComputed){
          ReferenceFieldSemantics referenceKind;
          TypeNode t = this.Type;
          if (t == null) return this.referenceSemantics;
          if (t is Struct){
            TypeNodeList args;
            bool b = t.IsAssignableToInstanceOf(SystemTypes.GenericIEnumerable, out args);
            if ( b && args!= null && args.Count > 0 && args[0] != null && args[0].IsObjectReferenceType)
              referenceKind = ReferenceFieldSemantics.EnumerableStructOfReferences;
            else if (t.IsAssignableTo(SystemTypes.IEnumerable))
              referenceKind = ReferenceFieldSemantics.EnumerableStructOfReferences;
            else
              referenceKind = ReferenceFieldSemantics.NonReference;
          }else if (t != null && t.IsObjectReferenceType)
            referenceKind = ReferenceFieldSemantics.Reference;
          else
            referenceKind = ReferenceFieldSemantics.NonReference;
          if (referenceKind == ReferenceFieldSemantics.NonReference)
            this.referenceSemantics = referenceKind | ReferenceFieldSemantics.None;
          else{
            if (this.GetAttribute(SystemTypes.LockProtectedAttribute) != null)
              this.referenceSemantics = referenceKind | ReferenceFieldSemantics.LockProtected;
            else if (this.GetAttribute(SystemTypes.ImmutableAttribute) != null)
              this.referenceSemantics = referenceKind | ReferenceFieldSemantics.Immutable;
            else if (this.GetAttribute(SystemTypes.RepAttribute) != null)
              this.referenceSemantics = referenceKind | ReferenceFieldSemantics.Rep;
            else if (this.GetAttribute(SystemTypes.PeerAttribute) != null)
              this.referenceSemantics = referenceKind | ReferenceFieldSemantics.Peer;
            else {
              ReferenceFieldSemantics r = ReferenceFieldSemantics.None;
              this.referenceSemantics = referenceKind | r;
            }
          }
        }
        return this.referenceSemantics;
      }
      set {
        this.referenceSemantics = value;
      }
    }
    public bool IsOwned{
      get{
        return this.IsRep || this.IsPeer;
      }
    }
      public bool IsOnce
      {
          get {
              return this.GetAttribute(SystemTypes.OnceAttribute) != null;
          }
      }

    public bool IsRep{
      get {
        return this.ReferenceSemantics == (ReferenceFieldSemantics.Rep | ReferenceFieldSemantics.Reference);
      }
    }
    public bool IsPeer {
      get {
        return this.ReferenceSemantics == (ReferenceFieldSemantics.Peer | ReferenceFieldSemantics.Reference);
      }
    }
    public bool IsLockProtected {
      get{
        return this.ReferenceSemantics == (ReferenceFieldSemantics.LockProtected | ReferenceFieldSemantics.Reference);
      }
    }
    public bool IsStrictReadonly {
      get {
        return this.GetAttribute(ExtendedRuntimeTypes.StrictReadonlyAttribute) != null;
      }
    }
#endif
        public override string ToString()
        {
            return DeclaringType.GetFullUnmangledNameWithTypeParameters() + "." + Name;
        }
#if FxCop
    internal override void GetName(MemberFormat options, StringBuilder name)
    {
      base.GetName(options, name);
      Method.AppendReturnType(options.ReturnType, this.Type, name);
    }
#endif
    }
#if ExtendedRuntime
  /// <summary>
  /// The behavior of a field in the Spec# object invariants/ownership/concurrency methodology.
  /// </summary>
  public enum ReferenceFieldSemantics{
    NotComputed,
    None,
    Rep,
    LockProtected,
    Immutable,
    Peer,
    SemanticsMask = 0xff,
    Reference = 0x100,
    EnumerableStructOfReferences = 0x200,
    NonReference = 0x300,
    ReferenceMask = 0xff00,
  }
#endif
#if FxCop
  public class PropertyNode : Member{
#else
    public class Property : Member
    {
#endif
#if !MinimalReader
        /// <summary>
        ///     The list of types (just one in C#) that contain abstract or virtual properties that are explicity implemented or
        ///     overridden by this property.
        /// </summary>
        public TypeNodeList ImplementedTypes;

        public TypeNodeList ImplementedTypeExpressions;
        public bool
            IsModelfield =
                false; //set to true if this property serves as the representation of a modelfield in an interface
#endif
#if FxCop
    public PropertyNode()
#else
        public Property()
#endif
            : base(NodeType.Property)
        {
        }
#if !MinimalReader
        public Property(TypeNode declaringType, AttributeList attributes, PropertyFlags flags, Identifier name,
            Method getter, Method setter)
            : base(declaringType, attributes, name, NodeType.Property)
        {
            Flags = flags;
            Getter = getter;
            Setter = setter;
            if (getter != null) getter.DeclaringMember = this;
            if (setter != null) setter.DeclaringMember = this;
        }
#endif
        public PropertyFlags Flags { get; set; }

        /// <summary>The method that is called to get the value of this property. Corresponds to the get clause in C#.</summary>
        public Method Getter { get; set; }

        /// <summary>The method that is called to set the value of this property. Corresponds to the set clause in C#.</summary>
        public Method Setter { get; set; }

        /// <summary>Other methods associated with the property. No equivalent in C#.</summary>
        public MethodList OtherMethods { get; set; }

        protected string fullName;
        public override string /*!*/ FullName
        {
            get
            {
                if (fullName != null) return fullName;
                var sb = new StringBuilder();
                sb.Append(DeclaringType.FullName);
                sb.Append('.');
                if (Name != null)
                    sb.Append(Name);
                var parameters = Parameters;
                for (int i = 0, n = parameters == null ? 0 : parameters.Count; i < n; i++)
                {
                    var par = parameters[i];
                    if (par == null || par.Type == null) continue;
                    if (i == 0)
                        sb.Append('(');
                    else
                        sb.Append(',');
                    sb.Append(par.Type.FullName);
                    if (i == n - 1)
                        sb.Append(')');
                }

                return fullName = sb.ToString();
            }
        }
#if !MinimalReader
        public virtual Method GetBaseGetter()
        {
            if (HidesBaseClassMember) return null;
            var t = DeclaringType;
            if (t == null) return null;
            while (t.BaseType != null)
            {
                t = t.BaseType;
                var mems = t.GetMembersNamed(Name);
                for (int i = 0, n = mems == null ? 0 : mems.Count; i < n; i++)
                {
                    var bprop = mems[i] as Property;
                    if (bprop == null) continue;
                    if (!bprop.ParametersMatch(Parameters)) continue;
                    if (bprop.Getter != null) return bprop.Getter;
                }
            }

            return null;
        }

        public virtual Method GetBaseSetter()
        {
            if (HidesBaseClassMember) return null;
            var t = DeclaringType;
            if (t == null) return null;
            while (t.BaseType != null)
            {
                t = t.BaseType;
                var mems = t.GetMembersNamed(Name);
                for (int i = 0, n = mems == null ? 0 : mems.Count; i < n; i++)
                {
                    var bprop = mems[i] as Property;
                    if (bprop == null) continue;
                    if (!bprop.ParametersMatch(Parameters)) continue;
                    if (bprop.Setter != null) return bprop.Setter;
                }
            }

            return null;
        }
#endif
#if !NoXml
        protected override Identifier GetDocumentationId()
        {
            var sb = new StringBuilder(DeclaringType.DocumentationId.ToString());
            sb[0] = 'P';
            sb.Append('.');
            if (Name != null)
                sb.Append(Name);
            var parameters = Parameters;
            for (int i = 0, n = parameters == null ? 0 : parameters.Count; i < n; i++)
            {
                var par = parameters[i];
                if (par == null || par.Type == null) continue;
                if (i == 0)
                    sb.Append('(');
                else
                    sb.Append(',');
                par.Type.AppendDocumentIdMangledName(sb, null, DeclaringType.TemplateParameters);
                if (i == n - 1)
                    sb.Append(')');
            }

            return Identifier.For(sb.ToString());
        }
#endif
#if !NoReflection
        public static Property GetProperty(PropertyInfo propertyInfo)
        {
            if (propertyInfo == null) return null;
            var tn = TypeNode.GetTypeNode(propertyInfo.DeclaringType);
            if (tn == null) return null;
            var paramInfos = propertyInfo.GetIndexParameters();
            var n = paramInfos == null ? 0 : paramInfos.Length;
            var parameterTypes = new TypeNode[n];
            if (paramInfos != null)
                for (var i = 0; i < n; i++)
                {
                    var param = paramInfos[i];
                    if (param == null) return null;
                    parameterTypes[i] = TypeNode.GetTypeNode(param.ParameterType);
                }

            return tn.GetProperty(Identifier.For(propertyInfo.Name), parameterTypes);
        }
#endif

#if !NoReflection
        protected PropertyInfo propertyInfo;

        public virtual PropertyInfo GetPropertyInfo()
        {
            if (propertyInfo == null)
            {
                if (DeclaringType == null) return null;
                var t = DeclaringType.GetRuntimeType();
                if (t == null) return null;
                if (Type == null) return null;
                var retType = Type.GetRuntimeType();
                if (retType == null) return null;
                var pars = Parameters;
                var n = pars == null ? 0 : pars.Count;
                var types = new Type[n];
                for (var i = 0; i < n; i++)
                {
                    var p = pars[i];
                    if (p == null || p.Type == null) return null;
                    var pt = types[i] = p.Type.GetRuntimeType();
                    if (pt == null) return null;
                }

                var members =
                    t.GetMember(Name.ToString(), MemberTypes.Property,
                        BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
                        BindingFlags.NonPublic);
                foreach (PropertyInfo prop in members)
                {
                    if (prop == null || prop.PropertyType != retType) continue;
                    var parameters = prop.GetIndexParameters();
                    if (parameters == null || parameters.Length != n) continue;
                    for (var i = 0; i < n; i++)
                    {
                        var parInfo = parameters[i];
                        if (parInfo == null || parInfo.ParameterType != types[i]) goto tryNext;
                    }

                    return propertyInfo = prop;
                    tryNext: ;
                }
            }

            return propertyInfo;
        }

        public virtual object GetValue(object targetObject, params object[] indices)
        {
            var propInfo = GetPropertyInfo();
            if (propInfo == null) throw new InvalidOperationException();
            return propInfo.GetValue(targetObject, indices);
        }

        public virtual Literal GetValue(Literal /*!*/ targetObject, params Literal[] indices)
        {
            var n = indices == null ? 0 : indices.Length;
            var inds = n == 0 ? null : new object[n];
            if (inds != null && indices != null)
                for (var i = 0; i < n; i++)
                {
                    var lit = indices[i];
                    inds[i] = lit == null ? null : lit.Value;
                }

            return new Literal(GetValue(targetObject.Value, inds));
        }

        public virtual void SetValue(object targetObject, object value, params object[] indices)
        {
            var propInfo = GetPropertyInfo();
            if (propInfo == null) throw new InvalidOperationException();
            propInfo.SetValue(targetObject, value, indices);
        }

        public virtual void SetValue(Literal /*!*/ targetObject, Literal /*!*/ value, params Literal[] indices)
        {
            var n = indices == null ? 0 : indices.Length;
            var inds = n == 0 ? null : new object[n];
            if (inds != null && indices != null)
                for (var i = 0; i < n; i++)
                {
                    var lit = indices[i];
                    inds[i] = lit == null ? null : lit.Value;
                }

            var propInfo = GetPropertyInfo();
            if (propInfo == null) throw new InvalidOperationException();
            propInfo.SetValue(targetObject.Value, value.Value, inds);
        }
#endif
#if !NoXml
        public override string HelpText
        {
            get
            {
                if (helpText != null)
                    return helpText;
                var sb = new StringBuilder(base.HelpText);
                // if there is already some help text, start the contract on a new line
                var startWithNewLine = sb.Length != 0;
                if (Getter != null && Getter.HelpText != null && Getter.HelpText.Length > 0)
                {
                    if (startWithNewLine)
                    {
                        sb.Append("\n");
                        startWithNewLine = false;
                    }

                    sb.Append("get\n");
                    var i = sb.Length;
                    sb.Append(Getter.HelpText);
                    if (sb.Length > i)
                        startWithNewLine = true;
                }

                if (Setter != null && Setter.HelpText != null && Setter.HelpText.Length > 0)
                {
                    if (startWithNewLine)
                    {
                        sb.Append("\n");
                        startWithNewLine = false;
                    }

                    sb.Append("set\n");
                    sb.Append(Setter.HelpText);
                }

                return helpText = sb.ToString();
            }
            set { base.HelpText = value; }
        }
#endif
        public override bool IsAssembly => Method.GetVisibilityUnion(Getter, Setter) == MethodFlags.Assembly;

        public override bool IsCompilerControlled =>
            Method.GetVisibilityUnion(Getter, Setter) == MethodFlags.CompilerControlled;

        public override bool IsFamily => Method.GetVisibilityUnion(Getter, Setter) == MethodFlags.Family;

        public override bool IsFamilyAndAssembly =>
            Method.GetVisibilityUnion(Getter, Setter) == MethodFlags.FamANDAssem;

        public override bool IsFamilyOrAssembly => Method.GetVisibilityUnion(Getter, Setter) == MethodFlags.FamORAssem;

        public bool IsFinal => (Getter == null || Getter.IsFinal) && (Setter == null || Setter.IsFinal);
        public override bool IsPrivate => Method.GetVisibilityUnion(Getter, Setter) == MethodFlags.Private;

        public override bool IsPublic => Method.GetVisibilityUnion(Getter, Setter) == MethodFlags.Public;
        public override bool IsSpecialName => (Flags & PropertyFlags.SpecialName) != 0;

        public override bool IsStatic => (Getter == null || Getter.IsStatic) && (Setter == null || Setter.IsStatic);

        /// <summary>
        ///     True if this property can in principle be overridden by a property in a derived class.
        /// </summary>
        public bool IsVirtual => (Getter == null || Getter.IsVirtual) && (Setter == null || Setter.IsVirtual);

        public override bool IsVisibleOutsideAssembly => (Getter != null && Getter.IsVisibleOutsideAssembly) ||
                                                         (Setter != null && Setter.IsVisibleOutsideAssembly);

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Property NotSpecified = new Property();

        public override Member HiddenMember
        {
            get { return HiddenProperty; }
            set { HiddenProperty = (Property)value; }
        }

        protected Property hiddenProperty;

        public virtual Property HiddenProperty
        {
            get
            {
                if (hiddenMember == NotSpecified) return null;
                var hiddenProperty = hiddenMember as Property;
                if (hiddenProperty != null) return hiddenProperty;

                var hiddenGetter = Getter == null ? null : Getter.HiddenMethod;
                var hiddenSetter = Setter == null ? null : Setter.HiddenMethod;
                var hiddenGetterProperty = hiddenGetter == null ? null : hiddenGetter.DeclaringMember as Property;
                var hiddenSetterProperty = hiddenSetter == null ? null : hiddenSetter.DeclaringMember as Property;
                hiddenProperty = hiddenGetterProperty;
                if (hiddenSetterProperty != null)
                    if (hiddenProperty == null ||
                        (hiddenSetterProperty.DeclaringType != null &&
                         hiddenSetterProperty.DeclaringType.IsDerivedFrom(hiddenProperty.DeclaringType)))
                        hiddenProperty = hiddenSetterProperty;
                hiddenMember = hiddenProperty;
                return hiddenProperty;
            }
            set { hiddenMember = value; }
        }

        public override Member OverriddenMember
        {
            get { return OverriddenProperty; }
            set { OverriddenProperty = (Property)value; }
        }

        protected Property overriddenProperty;

        public virtual Property OverriddenProperty
        {
            get
            {
                if (overriddenMember == NotSpecified) return null;
                var overriddenProperty = overriddenMember as Property;
                if (overriddenProperty != null) return overriddenProperty;

                var overriddenGetter = Getter == null ? null : Getter.OverriddenMethod;
                var overriddenSetter = Setter == null ? null : Setter.OverriddenMethod;
                var overriddenGetterProperty =
                    overriddenGetter == null ? null : overriddenGetter.DeclaringMember as Property;
                var overriddenSetterProperty =
                    overriddenSetter == null ? null : overriddenSetter.DeclaringMember as Property;
                overriddenProperty = overriddenGetterProperty;
                if (overriddenSetterProperty != null)
                    if (overriddenProperty == null ||
                        (overriddenSetterProperty.DeclaringType != null &&
                         overriddenSetterProperty.DeclaringType.IsDerivedFrom(overriddenProperty.DeclaringType)))
                        overriddenProperty = overriddenSetterProperty;
                overriddenMember = overriddenProperty;
                return overriddenProperty;
            }
            set { overriddenMember = value; }
        }

        private ParameterList parameters;

        /// <summary>
        ///     The parameters of this property if it is an indexer.
        /// </summary>
        public ParameterList Parameters
        {
            get
            {
                if (parameters != null) return parameters;
                if (Getter != null) return parameters = Getter.Parameters;
                var setterPars = Setter == null ? null : Setter.Parameters;
                var n = setterPars == null ? 0 : setterPars.Count - 1;
                var propPars = parameters = new ParameterList(n);
                if (setterPars != null)
                    for (var i = 0; i < n; i++)
                        propPars.Add(setterPars[i]);
                return propPars;
            }
            set { parameters = value; }
        }

        public virtual bool ParametersMatch(ParameterList parameters)
        {
            var pars = Parameters;
            var n = pars == null ? 0 : pars.Count;
            var m = parameters == null ? 0 : parameters.Count;
            if (n != m) return false;
            if (parameters == null) return true;
            for (var i = 0; i < n; i++)
            {
                var par1 = pars[i];
                var par2 = parameters[i];
                if (par1.Type != par2.Type) return false;
            }

            return true;
        }

        public virtual bool ParametersMatchStructurally(ParameterList parameters)
        {
            var pars = Parameters;
            var n = pars == null ? 0 : pars.Count;
            var m = parameters == null ? 0 : parameters.Count;
            if (n != m) return false;
            if (parameters == null) return true;
            for (var i = 0; i < n; i++)
            {
                var par1 = pars[i];
                var par2 = parameters[i];
                if (par1 == null || par2 == null) return false;
                if (par1.Type == null || par2.Type == null) return false;
                if (par1.Type != par2.Type && !par1.Type.IsStructurallyEquivalentTo(par2.Type)) return false;
            }

            return true;
        }

        public virtual bool ParameterTypesMatch(TypeNodeList argumentTypes)
        {
            var pars = Parameters;
            var n = pars == null ? 0 : pars.Count;
            var m = argumentTypes == null ? 0 : argumentTypes.Count;
            if (n != m) return false;
            if (argumentTypes == null) return true;
            for (var i = 0; i < n; i++)
            {
                var par = Parameters[i];
                if (par == null) return false;
                var argType = argumentTypes[i];
                if (par.Type != argType) return false;
            }

            return true;
        }

        protected TypeNode type;
        /// <summary>
        ///     The type of value that this property holds.
        /// </summary>
        public virtual TypeNode Type
        {
            get
            {
                if (type != null) return type;
                if (Getter != null) return type = Getter.ReturnType;
                if (Setter != null && Setter.Parameters != null)
                    return type = Setter.Parameters[Setter.Parameters.Count - 1].Type;
                return CoreSystemTypes.Object;
            }
            set { type = value; }
        }
#if !MinimalReader
        public TypeNode TypeExpression;
#endif
        public override string ToString()
        {
            return DeclaringType.GetFullUnmangledNameWithTypeParameters() + "." + Name;
        }
#if FxCop
    internal override void GetName(MemberFormat options, StringBuilder name)
    {
      base.GetName(options, name);
      ParameterCollection parameters = this.parameters.Count > 0 ? this.parameters : null;
      // AppendParametersAndReturnType will not emit the paramters
      // prefix and suffix if a null ParameterCollection is provided to it.
      // This prevents a parameterless property from being rendered as MyProperty[]
      Method.AppendParametersAndReturnType(options, parameters, '[', ']', this.Type, name);
    }
#endif
    }

    public class Variable : Expression
    {
#if !MinimalReader
        public TypeNode TypeExpression;
#endif
        public Variable(NodeType type)
            : base(type)
        {
        }

        /// <summary>The name of a stack location. For example the name of a local variable or the name of a method parameter.</summary>
        public Identifier Name { get; set; }
    }

    public class Parameter : Variable
    {
        /// <summary>The (C# custom) attributes of this parameter.</summary>
        public AttributeList Attributes { get; set; }

        /// <summary>
        ///     The value that should be supplied as the argument value of this optional parameter if the source code omits an
        ///     explicit argument value.
        /// </summary>
        public Expression DefaultValue { get; set; }

        public ParameterFlags Flags { get; set; }

        public MarshallingInformation MarshallingInformation { get; set; }

        public Method DeclaringMethod { get; set; }

        /// <summary>
        ///     Zero based index into a parameter list containing this parameter.
        /// </summary>
        public int ParameterListIndex { get; set; }

        /// <summary>
        ///     Zero based index into the list of arguments on the evaluation stack.
        ///     Instance methods have the this object as parameter zero, which means that the first parameter will have value 1,
        ///     not 0.
        /// </summary>
        public int ArgumentListIndex { get; set; }

        public Parameter()
            : base(NodeType.Parameter)
        {
        }

        public Parameter(Identifier name, TypeNode type)
            : base(NodeType.Parameter)
        {
            Name = name;
            Type = type;
        }
#if !MinimalReader
        public Parameter(AttributeList attributes, ParameterFlags flags, Identifier name, TypeNode type,
            Literal defaultValue, MarshallingInformation marshallingInformation)
            : base(NodeType.Parameter)
        {
            Attributes = attributes;
            DefaultValue = defaultValue;
            Flags = flags;
            MarshallingInformation = marshallingInformation;
            Name = name;
            Type = type;
        }
#endif
        /// <summary>
        ///     True if the corresponding argument value is used by the callee. (This need not be the case for a parameter marked
        ///     as IsOut.)
        /// </summary>
        public virtual bool IsIn
        {
            get { return (Flags & ParameterFlags.In) != 0; }
            set
            {
                if (value)
                    Flags |= ParameterFlags.In;
                else
                    Flags &= ~ParameterFlags.In;
            }
        }

        /// <summary>
        ///     True if the caller can omit providing an argument for this parameter.
        /// </summary>
        public virtual bool IsOptional
        {
            get { return (Flags & ParameterFlags.Optional) != 0; }
            set
            {
                if (value)
                    Flags |= ParameterFlags.Optional;
                else
                    Flags &= ~ParameterFlags.Optional;
            }
        }

        /// <summary>
        ///     True if the corresponding argument must be a left hand expression and will be updated when the call returns.
        /// </summary>
        public virtual bool IsOut
        {
            get { return (Flags & ParameterFlags.Out) != 0; }
            set
            {
                if (value)
                    Flags |= ParameterFlags.Out;
                else
                    Flags &= ~ParameterFlags.Out;
            }
        }
#if !MinimalReader
        protected internal TypeNode paramArrayElementType;

        /// <summary>
        ///     If the parameter is a param array, this returns the element type of the array. If not, it returns null.
        /// </summary>
        public virtual TypeNode GetParamArrayElementType()
        {
            var result = paramArrayElementType;
            if (result == null)
            {
                var attr = GetParamArrayAttribute();
                if (attr != null)
                {
                    var t = TypeNode.StripModifiers(Type);
                    var r = t as Reference;
                    if (r != null) t = r.ElementType;
                    var arr = t as ArrayType;
                    if (arr != null && arr.Rank == 1)
                        return paramArrayElementType = arr.ElementType;
                }

                paramArrayElementType = result = Class.DoesNotExist;
            }

            if (result == Class.DoesNotExist) return null;
            return result;
        }

        protected AttributeNode paramArrayAttribute;

        public virtual AttributeNode GetParamArrayAttribute()
        {
            var result = paramArrayAttribute;
            if (result == null)
            {
                var attributes = Attributes;
                for (int i = 0, n = attributes == null ? 0 : attributes.Count; i < n; i++)
                {
                    var attr = attributes[i];
                    if (attr == null) continue;
                    TypeNode attrType = null;
                    var mb = attr.Constructor as MemberBinding;
                    if (mb != null)
                    {
                        attrType = mb.BoundMember.DeclaringType;
                    }
                    else
                    {
                        var lit = attr.Constructor as Literal;
                        if (lit == null) continue;
                        attrType = lit.Value as TypeNode;
                    }

                    if (attrType == SystemTypes.ParamArrayAttribute)
                        return paramArrayAttribute = attr;
                }

                result = paramArrayAttribute = AttributeNode.DoesNotExist;
            }

            if (result == AttributeNode.DoesNotExist) return null;
            return result;
        }

        public override bool Equals(object obj)
        {
            var binding = obj as ParameterBinding;
            return obj == this || (binding != null && binding.BoundParameter == this);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        ///     Gets the first attribute of the given type in the attribute list of this parameter. Returns null if none found.
        ///     This should not be called until the AST containing this member has been processed to replace symbolic references
        ///     to members with references to the actual members.
        /// </summary>
        public virtual AttributeNode GetAttribute(TypeNode attributeType)
        {
            if (attributeType == null) return null;
            var attributes = Attributes;
            for (int i = 0, n = attributes == null ? 0 : attributes.Count; i < n; i++)
            {
                var attr = attributes[i];
                if (attr == null) continue;
                var mb = attr.Constructor as MemberBinding;
                if (mb != null)
                {
                    if (mb.BoundMember == null) continue;
                    if (mb.BoundMember.DeclaringType != attributeType) continue;
                    return attr;
                }

                var lit = attr.Constructor as Literal;
                if (lit == null) continue;
                if (lit.Value as TypeNode != attributeType) continue;
                return attr;
            }

            return null;
        }
#endif
#if ExtendedRuntime
    public virtual bool IsUniversallyDelayed {
      get {
        // Special handling of delegate constructors. Their first argument is delayed.
        if (this.DeclaringMethod != null && this.DeclaringMethod.DeclaringType is DelegateNode) {
          if (this.DeclaringMethod.Parameters[0] == this) { // first parameter (not including this)
            return true;
          }
        }
        return (this.GetAttribute(ExtendedRuntimeTypes.DelayedAttribute) != null);
      }
    }
#endif
        public override string ToString()
        {
            if (Name == null) return "";
            if (Type == null) return Name.ToString();
            return Type + " " + Name;
        }
    }
#if !MinimalReader
    public class ParameterBinding : Parameter, IUniqueKey
    {
        public Parameter /*!*/
            BoundParameter;

        public ParameterBinding(Parameter /*!*/ boundParameter, SourceContext sctx)
        {
            if (boundParameter == null) throw new ArgumentNullException("boundParameter");
            BoundParameter = boundParameter;
            SourceContext = sctx;
            Type = boundParameter.Type;
            Name = boundParameter.Name;
            TypeExpression = boundParameter.TypeExpression;
            Attributes = boundParameter.Attributes;
            DefaultValue = boundParameter.DefaultValue;
            Flags = boundParameter.Flags;
            MarshallingInformation = boundParameter.MarshallingInformation;
            DeclaringMethod = boundParameter.DeclaringMethod;
            ParameterListIndex = boundParameter.ParameterListIndex;
            ArgumentListIndex = boundParameter.ArgumentListIndex;
            //^ base();
        }

        int IUniqueKey.UniqueId => BoundParameter.UniqueKey;

        public override int GetHashCode()
        {
            return BoundParameter.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var pb = obj as ParameterBinding;
            if (pb != null)
                return BoundParameter.Equals(pb.BoundParameter);
            return BoundParameter.Equals(obj);
        }
    }
#endif
    public class Local : Variable
    {
        public Local()
            : base(NodeType.Local)
        {
        }

        public Local(TypeNode type)
            : base(NodeType.Local)
        {
            Name = Identifier.Empty;
            if (type == null) type = CoreSystemTypes.Object;
            Type = type;
        }

        public Local(Identifier name, TypeNode type)
            : this(type)
        {
            Name = name;
        }

        public bool Pinned { get; set; }
#if !MinimalReader
        public Block DeclaringBlock;
        public bool InitOnly;
        public int Index;
#endif
#if !MinimalReader
        public Local(TypeNode type, SourceContext context)
            : this(Identifier.Empty, type, null)
        {
            SourceContext = context;
        }

        public Local(Identifier name, TypeNode type, SourceContext context)
            : this(name, type, null)
        {
            SourceContext = context;
        }

        public Local(Identifier name, TypeNode type, Block declaringBlock)
            : base(NodeType.Local)
        {
            DeclaringBlock = declaringBlock;
            Name = name;
            if (type == null) type = CoreSystemTypes.Object;
            Type = type;
        }
#endif
#if !MinimalReader
        public override bool Equals(object obj)
        {
            var binding = obj as LocalBinding;
            return obj == this || (binding != null && binding.BoundLocal == this);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            if (Name == null) return "No name";
            return Name.ToString();
        }

        public uint Attributes; // from pdb
        public bool HasNoPDBInfo;
#endif
    }
#if !MinimalReader
    public class LocalBinding : Local, IUniqueKey
    {
        public Local /*!*/
            BoundLocal;

        public LocalBinding(Local /*!*/ boundLocal, SourceContext sctx)
        {
            if (boundLocal == null) throw new ArgumentNullException("boundLocal");
            BoundLocal = boundLocal;
            //^ base();
            SourceContext = sctx;
            Type = boundLocal.Type;
            Name = boundLocal.Name;
            TypeExpression = boundLocal.TypeExpression;
            DeclaringBlock = boundLocal.DeclaringBlock;
            Pinned = boundLocal.Pinned;
            InitOnly = boundLocal.InitOnly;
            Index = boundLocal.Index;
        }

        int IUniqueKey.UniqueId => BoundLocal.UniqueKey;

        public override int GetHashCode()
        {
            return BoundLocal.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var lb = obj as LocalBinding;
            if (lb != null)
                return BoundLocal.Equals(lb.BoundLocal);
            return BoundLocal.Equals(obj);
        }
    }
#endif
    /// <summary>
    ///     A named container of types and nested namespaces.
    ///     The name of the container implicitly qualifies the names of the contained types and namespaces.
    /// </summary>
    public class Namespace : Member
    {
        /// <summary>The FullName of the namespace in the form of an Identifier rather than in the form of a string.</summary>
        public Identifier FullNameId;
#if !MinimalReader && !CodeContracts
    /// <summary>
    /// Provides alternative names for types and nested namespaces. Useful for introducing shorter names or for resolving name clashes.
    /// The names should be added to the scope associated with this namespace.
    /// </summary>
    public AliasDefinitionList AliasDefinitions;
    /// <summary>
    /// The list of namespaces that are fully contained inside this namespace.
    /// </summary>
    public NamespaceList NestedNamespaces;
    /// <summary>
    /// The Universal Resource Identifier that should be associated with all declarations inside this namespace.
    /// Typically used when the types inside the namespace are serialized as an XML Schema Definition. (XSD)
    /// </summary>
    public Identifier URI;
    /// <summary>
    /// The list of the namespaces of types that should be imported into the scope associated with this namespace.
    /// </summary>
    public UsedNamespaceList UsedNamespaces;
#endif
        /// <summary>
        ///     A delegate that is called the first time Types is accessed. Provides for incremental construction of the namespace
        ///     node.
        /// </summary>
        public TypeProvider ProvideTypes;

        /// <summary>
        ///     Opaque information passed as a parameter to the delegate in ProvideTypes. Typically used to associate this
        ///     namespace
        ///     instance with a helper object.
        /// </summary>
        public object ProviderHandle;

        /// <summary>
        ///     A method that fills in the Types property of the given namespace. Must not leave Types null.
        /// </summary>
        public delegate void TypeProvider(Namespace @namespace, object handle);

        protected string fullName;
        protected TypeNodeList types;

        public Namespace()
            : base(NodeType.Namespace)
        {
        }

        public Namespace(Identifier name)
            : base(NodeType.Namespace)
        {
            Name = name;
            FullNameId = name;
            if (name != null)
                fullName = name.ToString();
        }
#if !MinimalReader && !CodeContracts
    public Namespace(Identifier name, TypeProvider provideTypes, object providerHandle)
      : base(NodeType.Namespace){
      this.Name = name;
      this.FullNameId = name;
      if (name != null)
        this.fullName = name.ToString();
      this.ProvideTypes = provideTypes;
      this.ProviderHandle = providerHandle;
    }
    public Namespace(Identifier name, Identifier fullName, AliasDefinitionList aliasDefinitions,  UsedNamespaceList usedNamespaces, 
      NamespaceList nestedNamespaces, TypeNodeList types)
      : base(NodeType.Namespace){
      this.Name = name;
      this.FullNameId = fullName;
      if (fullName != null)
        this.fullName = fullName.ToString();
      this.AliasDefinitions = aliasDefinitions;
      this.NestedNamespaces = nestedNamespaces;
      this.Types = types;
      this.UsedNamespaces = usedNamespaces;
    }
#endif
        public override string /*!*/ FullName => fullName == null ? "" : fullName;
        public override bool IsAssembly => false;
        public override bool IsCompilerControlled => false;
        public override bool IsFamily => false;
        public override bool IsFamilyAndAssembly => false;
        public override bool IsFamilyOrAssembly => false;
        public override bool IsPrivate => !IsPublic;
        public override bool IsPublic => isPublic;
        protected internal bool isPublic;
        public override bool IsSpecialName => false;
        public override bool IsStatic => false;
        public override bool IsVisibleOutsideAssembly => false;

        /// <summary>
        ///     The list of types contained inside this namespace. If the value of Types is null and the value of ProvideTypes is
        ///     not null, the
        ///     TypeProvider delegate is called to fill in the value of this property.
        /// </summary>
        public TypeNodeList Types
        {
            get
            {
                if (types == null)
                    if (ProvideTypes != null)
                        lock (this)
                        {
                            if (types == null) ProvideTypes(this, ProviderHandle);
                        }
                    else
                        types = new TypeNodeList();

                return types;
            }
            set { types = value; }
        }
    }
#if !MinimalReader && !CodeContracts
  /// <summary>
  /// The root node of an Abstract Syntax Tree. Typically corresponds to multiple source files compiled to form a single target.
  /// </summary>
  public class Compilation : Node{
    /// <summary>
    /// The compilation parameters that are used for this compilation.
    /// </summary>
    public System.CodeDom.Compiler.CompilerParameters CompilerParameters;
    /// <summary>
    /// The target code object that is produced as a result of this compilation.
    /// </summary>
    public Module TargetModule;
    /// <summary>
    /// A list of all the compilation units (typically source files) that make up this compilation.
    /// </summary>
    public CompilationUnitList CompilationUnits;
    /// <summary>
    /// A scope for symbols that belong to the compilation as a whole. No C# equivalent. Null if not applicable.
    /// </summary>
    public Scope GlobalScope;
    /// <summary>
    /// A list of compilations that produce assemblies and modules that are referenced by this compilation and hence need to be
    /// compiled before this Compilation is compiled. This list is not intended to include already compiled framework assemblies
    /// such as system.dll.
    /// </summary>
    public CompilationList ReferencedCompilations;
    public DateTime LastModified = DateTime.Now;
    public DateTime LastCompiled = DateTime.MinValue;

    public Compilation()
      : base(NodeType.Compilation){
    }
    public Compilation(Module targetModule, CompilationUnitList compilationUnits, System.CodeDom.Compiler.CompilerParameters compilerParameters, Scope globalScope)
      : base(NodeType.Compilation){
      this.CompilationUnits = compilationUnits;
      this.TargetModule = targetModule;
      this.CompilerParameters = compilerParameters;
      this.GlobalScope = globalScope;
    }
    public virtual Compilation CloneCompilationUnits(){
      Compilation clone = (Compilation)base.Clone();
      CompilationUnitList cus = this.CompilationUnits;
      if (cus != null){
        clone.CompilationUnits = cus = cus.Clone();
        for (int i = 0, n = cus.Count; i < n; i++){
          CompilationUnit cu = cus[i];
          if (cu == null) continue;
          cus[i] = cu = (CompilationUnit)cu.Clone();
          cu.Compilation = clone;
          cu.Nodes = null;
        }
      }
      return clone;
    }
  }
  /// <summary>
  /// The root node of an Abstract Syntax Tree. Corresponds to the starting production of the syntax. Equivalent to C# compilation-unit.
  /// Typically a compilation unit corresponds to a single source file.
  /// </summary>
  public class CompilationUnit : Node{
    /// <summary>
    /// An identifier that can be used to retrieve the source text of the compilation unit.
    /// </summary>
    public Identifier Name;
    /// <summary>
    /// An anonymous (name is Identifier.Empty) namespace holding types and nested namespaces.
    /// </summary>
    public NodeList Nodes;
    /// <summary>
    /// The preprocessor symbols that are to treated as defined when compiling this CompilationUnit into the TargetModule.
    /// </summary>
    public Hashtable PreprocessorDefinedSymbols;
    /// <summary>
    /// Pragma warning information.
    /// </summary>
    public TrivialHashtable PragmaWarnInformation;
    /// <summary>
    /// The compilation of which this unit forms a part.
    /// </summary>
    public Compilation Compilation;

    public CompilationUnit()
      : base(NodeType.CompilationUnit){
    }
    public CompilationUnit(Identifier name)
      : base(NodeType.CompilationUnit){
      this.Name = name;
    }
  }
  public class CompilationUnitSnippet : CompilationUnit{
    public DateTime LastModified = DateTime.Now;
    public IParserFactory ParserFactory;
    public Method ChangedMethod;
    public int OriginalEndPosOfChangedMethod;

    public CompilationUnitSnippet(){
      this.NodeType = NodeType.CompilationUnitSnippet;
    }
    public CompilationUnitSnippet(Identifier name, IParserFactory parserFactory, SourceContext sctx){
      this.NodeType = NodeType.CompilationUnitSnippet;
      this.Name = name;
      this.ParserFactory = parserFactory;
      this.SourceContext = sctx;
    }
  }
  public abstract class Composer{
    public abstract Node Compose (Node node, Composer context, bool hasContextReference, Class scope);
    private class NullComposer: Composer{
      public override Node Compose(Node node, Composer context, bool hasContextReference, Class scope){
        return node;
      }
    }
    public static readonly Composer Null = new NullComposer();
  }
  public class Composition: Expression{
    public Expression Expression;
    public Composer Composer;
    public Class Scope;
    public Composition(Expression exp, Composer composer, Class scope)
      : base(NodeType.Composition){
      this.Expression = exp;
      this.Composer = composer;
      this.Scope = scope;
      if (exp != null) this.Type = exp.Type;
    }
  }
#endif
#if ExtendedRuntime
  // query nodes
  public class QueryAlias: QueryExpression{
    public Identifier Name;
    public Expression Expression;
    public QueryAlias(): base(NodeType.QueryAlias){
    }
  }   
  public abstract class Accessor{
  }
  public class MemberAccessor: Accessor{
    public Member Member;
    public TypeNode Type;
    public bool Yield;
    public Accessor Next;
    public MemberAccessor(Member member){
      this.Member = member;
    }
  }
  public class SequenceAccessor: Accessor{
    public ArrayList Accessors; // member accessors only
    public SequenceAccessor(){
      this.Accessors = new ArrayList();
    }
  }
  public class SwitchAccessor: Accessor{
    public TypeUnion Type;
    public Hashtable Accessors;  // key == type
    public SwitchAccessor(){
      this.Accessors = new Hashtable();
    }
  }
  public enum Cardinality{
    None,       // reference type
    One,        // !
    ZeroOrOne,  // ?
    OneOrMore,  // +
    ZeroOrMore  // *
  }
  public class QueryAxis: QueryExpression{
    public Expression Source;
    public bool IsDescendant;
    public Identifier Name;
    public Identifier Namespace;
    public TypeNode TypeTest;
    public Accessor AccessPlan;
    public Cardinality Cardinality;
    public int YieldCount;
    public TypeNodeList YieldTypes;
    public bool IsCyclic;
    public bool IsIterative;
    public QueryAxis (Expression source, bool isDescendant, Identifier name, TypeNode typeTest)
      : base(NodeType.QueryAxis){
      this.Source = source;
      this.IsDescendant = isDescendant;
      this.Name = name;
      this.TypeTest = typeTest;
    }
  }   
  public class QueryAggregate: QueryExpression{ 
    public Identifier Name;
    public TypeNode AggregateType;
    public Expression Expression;
    public ContextScope Context;
    public QueryGroupBy Group;
    public QueryAggregate(): base(NodeType.QueryAggregate){
    }
  }
  public class ContextScope{
    public ContextScope Previous;
    public TypeNode Type;
    public Expression Target;
    public Expression Position;
    public Block PreFilter;
    public Block PostFilter;
    public ContextScope(ContextScope previous, TypeNode type){
      this.Previous = previous;
      this.Type = type;
    }
  }
  public class QueryContext: QueryExpression{
    public ContextScope Scope;
    public QueryContext()
      : base(NodeType.QueryContext){
    }    
    public QueryContext(ContextScope scope): base(NodeType.QueryContext){
      this.Scope = scope;
      if (scope != null) this.Type = scope.Type;
    }    
  }   
  public class QueryDelete: QueryExpression{
    public Expression Source;    
    public Expression Target;
    public ContextScope Context;
    public Expression SourceEnumerable;
    public QueryDelete(): base(NodeType.QueryDelete){      
    }
  } 
  public class QueryDistinct: QueryExpression{
    public Expression Source;
    public ContextScope Context;
    public QueryGroupBy Group;
    public Expression GroupTarget;
    public QueryDistinct(): base(NodeType.QueryDistinct){
    }
  }  
  public class QueryDifference: QueryExpression{
    public Expression LeftSource;
    public Expression RightSource;
    public QueryDifference() : base(NodeType.QueryDifference){
    }
  }  
  public class QueryExists: QueryExpression{
    public Expression Source;
    public QueryExists() : base(NodeType.QueryExists){
    }
  }  
  public abstract class QueryExpression: Expression{
    protected QueryExpression(NodeType nt): base(nt){
    }
  }
  public class QueryFilter: QueryExpression{
    public Expression Source;
    public Expression Expression;
    public ContextScope Context;
    public QueryFilter(): base(NodeType.QueryFilter){
    }    
    public QueryFilter (Expression source, Expression filter): this(){
      this.Source = source;
      this.Expression = filter;
    }
  } 
  public class QueryYielder: Statement{
    public Expression Source;
    public Expression Target;
    public Expression State;
    public Block Body;
    public QueryYielder(): base(NodeType.QueryYielder){
    }
  }
  public class QueryGeneratedType: Statement{
    public TypeNode Type;
    public QueryGeneratedType(TypeNode type): base(NodeType.QueryGeneratedType){
      this.Type = type;
    }
  }
  public class QueryGroupBy: QueryExpression{
    public Expression Source;
    public ContextScope GroupContext;
    public ExpressionList GroupList;
    public ExpressionList AggregateList;
    public Expression Having;
    public ContextScope HavingContext;
    public QueryGroupBy(): base(NodeType.QueryGroupBy){
      this.GroupList = new ExpressionList();
      this.AggregateList = new ExpressionList();
    }
  }  
  public class QueryInsert: QueryExpression{
    public Expression Location;
    public QueryInsertPosition Position;
    public ExpressionList InsertList;
    public ExpressionList HintList;
    public ContextScope Context;
    public bool IsBracket;
    public QueryInsert(): base(NodeType.QueryInsert){
      this.InsertList = new ExpressionList();
      this.HintList = new ExpressionList();
    }
  }    
  public enum QueryInsertPosition{
    After,
    At,
    Before,
    First,
    In,
    Last
  }  
  public class QueryIntersection: QueryExpression{
    public Expression LeftSource;
    public Expression RightSource;
    public QueryIntersection(): base(NodeType.QueryIntersection){
    }
  }
  public class QueryScope: BlockScope{
    public QueryScope(Scope/*!*/ parentScope)
      : base(parentScope, null) {
    }
  }
  public class QueryIterator: QueryAlias{
    public TypeNode ElementType;
    public TypeNode TypeExpression;
    public ExpressionList HintList;
    public QueryIterator(): base(){
      this.NodeType = NodeType.QueryIterator;      
      this.HintList = new ExpressionList();
    }
  }   
  public class QueryJoin: QueryExpression{
    public Expression LeftOperand;
    public Expression RightOperand;
    public QueryJoinType JoinType;
    public Expression JoinExpression;
    public ContextScope JoinContext;
    public QueryJoin(): base(NodeType.QueryJoin){
    }
  }  
  public enum QueryJoinType{
    Inner,
    LeftOuter,
    RightOuter,
    FullOuter
  }
  public class QueryLimit: QueryExpression{
    public Expression Source;
    public Expression Expression;
    public bool IsPercent;
    public bool IsWithTies;
    public QueryLimit(): base(NodeType.QueryLimit){
    }
  }  
  public class QueryOrderBy: QueryExpression{
    public Expression Source;
    public ContextScope Context;
    public ExpressionList OrderList;
    public QueryOrderBy(): base(NodeType.QueryOrderBy){
      this.OrderList = new ExpressionList();
    }
  }
  public enum QueryOrderType{
    Ascending,
    Descending,
    Document
  }  
  public class QueryOrderItem: QueryExpression{
    public Expression Expression;
    public QueryOrderType OrderType = QueryOrderType.Ascending;
    public QueryOrderItem(): base(NodeType.QueryOrderItem){
    }
  }  
  public class QueryPosition: QueryExpression{
    public ContextScope Context;
    public QueryPosition(ContextScope context): base(NodeType.QueryPosition){
      this.Context = context;
      this.Type = CoreSystemTypes.Int32;
    }
    public static readonly Identifier Id = Identifier.For("position");
  } 
  public class QueryProject: QueryExpression{
    public Expression Source;
    public ContextScope Context;
    public ExpressionList ProjectionList;
    public TypeNode ProjectedType;
    public MemberList Members;
    public QueryProject(): base(NodeType.QueryProject){
      this.ProjectionList = new ExpressionList();
    }
  }   
  public class QueryQuantifiedExpression: QueryExpression{
    public QueryQuantifier Left;
    public QueryQuantifier Right;
    public Expression Expression;
    public QueryQuantifiedExpression(): base(NodeType.QueryQuantifiedExpression){
    }
  }
  public class QueryQuantifier: QueryExpression{
    public Expression Expression;
    public Expression Target;
    public QueryQuantifier(NodeType nt): base(nt){
    }
  }
  public class QuerySingleton: QueryExpression{
    public Expression Source;
    public QuerySingleton(): base(NodeType.QuerySingleton){
    }
  }
  public class QuerySelect: QueryExpression{
    public Expression Source;
    public QueryCursorDirection Direction;
    public QueryCursorAccess Access;
    public QuerySelect(Expression source): base(NodeType.QuerySelect){
      if (source != null){
        this.Source = source;
        this.Type = source.Type;
      }
    }
  } 
  public enum QueryCursorDirection{
    ForwardOnly,
    Scrollable
  }  
  public enum QueryCursorAccess{
    ReadOnly,
    Updatable
  }  

  public abstract class QueryStatement: Statement{
    protected QueryStatement(NodeType nt): base(nt){
    }
  }  
  public class QueryTypeFilter: QueryExpression{
    public Expression Source;
    public TypeNode Constraint;
    public QueryTypeFilter(): base(NodeType.QueryTypeFilter){     
    }
  }   
  public class QueryUnion: QueryExpression{
    public Expression LeftSource;
    public Expression RightSource;
    public QueryUnion() : base(NodeType.QueryUnion){
    }
  }  
  public class QueryUpdate: QueryExpression{
    public Expression Source;
    public ExpressionList UpdateList;
    public ContextScope Context;
    public QueryUpdate() : base(NodeType.QueryUpdate){
      this.UpdateList = new ExpressionList();
    }
  }
  public class QueryTransact: Statement{
    public Expression Source;
    public Expression Isolation;
    public Block Body;
    public Block CommitBody;
    public Block RollbackBody;
    public Expression Transaction;
    public QueryTransact(): base(NodeType.QueryTransact){
    }
  }
  public class QueryCommit: Statement{
    public QueryCommit(): base(NodeType.QueryCommit){
    }
  }
  public class QueryRollback: Statement{
    public QueryRollback(): base(NodeType.QueryRollback){
    }
  }
#endif
#if !MinimalReader
    /// <summary>
    ///     An object that knows how to produce a particular scope's view of a type.
    /// </summary>
    public class TypeViewer
    {
        /// <summary>
        ///     Return a scope's view of the argument type, where the scope's view is represented
        ///     by a type viewer.
        ///     [The identity function, except for dialects (e.g. Extensible Sing#) that allow
        ///     extensions and differing views of types].
        ///     Defined as a static method to allow the type viewer to be null,
        ///     meaning an identity-function view.
        /// </summary>
        public static TypeNode /*!*/ GetTypeView(TypeViewer typeViewer, TypeNode /*!*/ type)
        {
            return typeViewer == null ? type.EffectiveTypeNode : typeViewer.GetTypeView(type);
        }

        /// <summary>
        ///     Return the typeViewer's view of the argument type.  Overridden by subclasses
        ///     that support non-identity-function type viewers, e.g. Extensible Sing#.
        /// </summary>
        protected virtual TypeNode /*!*/ GetTypeView(TypeNode /*!*/ type)
        {
            return type.EffectiveTypeNode;
        }
    }
#endif
#if WHIDBEY
    internal static
#endif
        class PlatformHelpers
    {
        internal static bool TryParseInt32(string s, out int result)
        {
#if WHIDBEY
            return int.TryParse(s, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out result);
#else
      result = 0;
      bool succeeded = false;
      try {
        result = Int32.Parse(s, NumberFormatInfo.InvariantInfo);
        succeeded = true;
      }catch(ArgumentException){
      }catch(FormatException){
      }catch(OverflowException){}
      return succeeded;
#endif
        }

        internal static int StringCompareOrdinalIgnoreCase(string strA, int indexA, string strB, int indexB, int length)
        {
#if WHIDBEY
            return string.Compare(strA, indexA, strB, indexB, length, StringComparison.OrdinalIgnoreCase);
#else
      return string.Compare(strA, indexA, strB, indexB, length, true, CultureInfo.InvariantCulture);
#endif
        }

        internal static int StringCompareOrdinalIgnoreCase(string strA, string strB)
        {
#if WHIDBEY
            return string.Compare(strA, strB, StringComparison.OrdinalIgnoreCase);
#else
      return string.Compare(strA, strB, true, CultureInfo.InvariantCulture);
#endif
        }
    }
#if FxCop
  public class CatchNode : Statement{
    private Block block;
    private TypeNode type;
    private Expression variable;
    private Filter filter;
    internal int handlerEnd;
    internal CatchNode()
      : base(NodeType.Catch){
    }
    internal CatchNode(Block block, Expression variable, TypeNode type)
      : this(block, variable, type, null){
    }
    internal CatchNode(Block block, Expression variable, TypeNode type, Filter filter)
      : base(NodeType.Catch){
      this.block = block;
      this.variable = variable;
      this.type = type;
      this.filter = filter;
    }
    public Block Block{
      get{return this.block;}
      internal set{this.block = value;}
    }
    public Filter Filter{
      get{return this.filter;}
      internal set{this.filter = value;}
    }
    public TypeNode Type{
      get{return this.type;}
      internal set{this.type = value;}
    }
    public Expression Variable{
      get{return this.variable;}
      internal set{this.variable = value;}
    }
  }
  public class FinallyNode : Statement{
    private Block block;
    internal int handlerEnd;
    internal FinallyNode()
      : base(NodeType.Finally){
    }
    internal FinallyNode(Block block)
      : base(NodeType.Finally){
      this.block = block;
    }
    public Block Block{
      get{return this.block;}
      internal set{this.block = value;}
    }
  }
  public class TryNode : Statement {
    private CatchNodeCollection catchers = new CatchNodeCollection();
    private FaultHandler faultHandler;
    private FinallyNode finallyClause;
    private Block block;
    internal TryNode()
      : base(NodeType.Try) {
    }
    internal TryNode(Block block, CatchNodeCollection catchers, FaultHandler faultHandler, FinallyNode @finally)
      : base(NodeType.Try) {
      this.catchers = catchers;
      this.faultHandler = faultHandler;
      this.finallyClause = @finally;
      this.block = block;
    }
    internal int tryEnd;
    internal int handlersEnd;
    public CatchNodeCollection Catchers {
      get { return this.catchers; }
      internal set { this.catchers = value; }
    }
    public FaultHandler FaultHandler {
      get { return this.faultHandler; }
      internal set { this.faultHandler = value; }
    }
    public FinallyNode Finally {
      get { return this.finallyClause; }
      internal set { this.finallyClause = value; }
    }
    public Block Block {
      [DebuggerStepThrough] get { return this.block; }
      [DebuggerStepThrough] internal set { this.block = value; }
    }
  }
    public abstract class FormatOptions
    {
        internal Options m_options;

        protected FormatOptions() { }

        internal void SetOptions(Options options, bool enable)
        {
            if (enable)
            {
                this.m_options |= options;
                return;
            }
            this.m_options &= ~options;
        }

        internal bool IsSet(Options options)
        {
            return (this.m_options & options) == options;
        }

        [Flags]
        internal enum Options
        {
            None = 0x0,
            InsertSpacesBetweenParameters = 0x1,
            InsertSpacesBetweenTypeParameters = 0x2,
            InsertSpacesBetweenMethodTypeParameters = 0x4,
            ShowGenericTypeArity = 0x8,
            ShowGenericMethodTypeParameterNames = 0x10,
            ShowGenericTypeParameterNames = 0x20,
            ShowTypeModifiers = 0x40,
            ShowParameterNames = 0x80
        }
    }
    internal class MemberFormat : FormatOptions
    {
        TypeFormat m_declaringTypeFormat;
        TypeFormat m_returnTypeFormat;
        ParameterFormat m_parameterFormat;
    
        public MemberFormat()
        {
            this.m_declaringTypeFormat = new TypeFormat();
            this.m_returnTypeFormat = new TypeFormat();
            this.m_parameterFormat = new ParameterFormat();
        }
    
        public TypeFormat Type
        {
            get { return this.m_declaringTypeFormat; }
        }
    
        public TypeFormat ReturnType
        {
            get { return this.m_returnTypeFormat; }
        }
    
        public ParameterFormat Parameters
        {
            get { return this.m_parameterFormat; }
        }
    
        public bool ShowGenericMethodTypeParameterNames
        {
            get { return IsSet(Options.ShowGenericMethodTypeParameterNames); }
            set { SetOptions(Options.ShowGenericMethodTypeParameterNames, value); }
        }
    
        public bool InsertSpacesBetweenMethodTypeParameters
        {
            get { return IsSet(Options.InsertSpacesBetweenMethodTypeParameters); }
            set { SetOptions(Options.InsertSpacesBetweenMethodTypeParameters, value); }
        }
    }
    internal class ParameterFormat : TypeFormat
    {
        public ParameterFormat() { }
    
        public bool InsertSpacesBetweenParameters
        {
            get { return IsSet(Options.InsertSpacesBetweenParameters); }
            set { SetOptions(Options.InsertSpacesBetweenParameters, value); }
        }
    
        public bool ShowParameterNames
        {
            get { return IsSet(Options.ShowParameterNames); }
            set { SetOptions(Options.ShowParameterNames, value); }
        }
    }
    internal class TypeFormat : FormatOptions
    {
        private TypeNameFormat m_typeName;
        public TypeFormat() { }

        public TypeFormat Clone()
        {
            TypeFormat clone = new TypeFormat();
            clone.m_typeName = this.m_typeName;
            clone.m_options = this.m_options;
            return clone;
        }

        public bool InsertSpacesBetweenTypeParameters
        {
            get { return IsSet(Options.InsertSpacesBetweenTypeParameters); }
            set { SetOptions(Options.InsertSpacesBetweenTypeParameters, value); }
        }

        public bool ShowGenericTypeArity
        {
            get { return IsSet(Options.ShowGenericTypeArity); }
            set { SetOptions(Options.ShowGenericTypeArity, value); }
        }

        public bool ShowGenericTypeParameterNames
        {
            get { return IsSet(Options.ShowGenericTypeParameterNames); }
            set { SetOptions(Options.ShowGenericTypeParameterNames, value); }
        }

        public bool ShowTypeModifiers
        {
            get { return IsSet(Options.ShowTypeModifiers); }
            set { SetOptions(Options.ShowTypeModifiers, value); }
        }

        public TypeNameFormat TypeName
        {
            get { return this.m_typeName; }
            set { this.m_typeName = value; }
        }
    }
    internal enum TypeNameFormat
    {
        None = 0,
        InnermostNested,    
        Short,
        FullyQualified
    }
#endif
}