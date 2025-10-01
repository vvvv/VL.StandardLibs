using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VL.Core
{
    public enum StringSubType { SingleLine, Multiline, Filename, Directory, URL, IP }
    public enum StringType { String, Comment, Link } //NumberedComment
    public enum PathType { File, Directory }
}
