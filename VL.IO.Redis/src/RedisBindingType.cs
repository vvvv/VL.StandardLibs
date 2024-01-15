namespace VL.IO.Redis
{
    public enum RedisBindingType 
    {
        None = 0,
        Send = 1,
        Receive = 2,
        SendAndReceive = Send | Receive,
        AlwaysReceive = 8,
    }
}
