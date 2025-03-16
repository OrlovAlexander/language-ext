using System.IO;
using System.Buffers;
using LanguageExt.Pipes;
using System.Collections.Generic;
using LanguageExt.Traits;
using static LanguageExt.Prelude;
using LanguageExt.UnsafeValueAccess;

namespace LanguageExt.Sys.IO;

public static class Stream<M> where M : MonadIO<M>
{
    /// <summary>
    /// Get a pipe of chunks from a Stream
    /// </summary>
    public static PipeT<Stream, SeqLoan<byte>, M, Unit> read(int chunkSize)
    {
        return from fs in PipeT.awaiting<M, Stream, SeqLoan<byte>>()
               from _  in PipeT.yieldAll<M, Stream, SeqLoan<byte>>(chunks(fs, chunkSize))
               select unit;

        static async IAsyncEnumerable<SeqLoan<byte>> chunks(Stream fs, int chunkSize)
        {
            var pool = ArrayPool<byte>.Shared;
            while (true)
            {
                var buffer = pool.Rent(chunkSize);
                var count  = await fs.ReadAsync(buffer, 0, chunkSize).ConfigureAwait(false);
                if (count < 1)
                {
                    pool.Return(buffer);
                    yield break;
                }
                yield return buffer.ToSeqLoanUnsafe(count, pool); 
            }
        }
    }
}
