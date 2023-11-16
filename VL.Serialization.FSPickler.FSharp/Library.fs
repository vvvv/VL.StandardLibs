namespace VL.Serialization.FSPickler.FSharp

open MBrace.FsPickler
open MBrace.FsPickler.CSharpProxy
open MBrace.FsPickler.Combinators

type PicklerFactory =
    static member CreatePickler<'a>(reader: System.Func<ReadState, 'a>, writer: System.Action<WriteState, 'a>, ?useWithSubtypes) = 
        Pickler.FromPrimitives(reader.ToFSharpFunc(), writer.ToFSharpFunc(), ?useWithSubtypes = useWithSubtypes)

    static member CreateWrapPickler<'a, 'b>(recover: System.Func<'a, 'b>, convert: System.Func<'b, 'a>, p) =
        Pickler.wrap (recover.ToFSharpFunc()) (convert.ToFSharpFunc()) p