#nullable enable

namespace VL.IO.Redis
{
    interface IParticipant
    {
        void BuildUp(TransactionBuilder builder);
        void Invalidate(string key);
    }
}
