namespace Utils;
public class UpdateIdentifier
{
    public string _transactionManagerId;
    public int _sequenceNumber;
    public UpdateIdentifier(string transactionManagerId, int sequenceNumber)
    {
        _transactionManagerId = transactionManagerId;
        _sequenceNumber = sequenceNumber;
    }
    public static UpdateIdentifier FromProtobuf(UpdateId updateId)
    {
        return new UpdateIdentifier(updateId.TransactionManagerId, updateId.SequenceNumber);
    }
}