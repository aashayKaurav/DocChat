namespace DocChat.Domain.Enums;

public enum DocumentStatus
{
    Uploaded = 0,
    Processing = 1,
    Ready = 2,
    Failed = 3
}

public enum MessageRole
{
    User = 0,
    Assistant = 1
}
