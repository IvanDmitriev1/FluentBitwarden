namespace BitwardenApi.Notifications.Models;

public enum PushNotificationLogOutReasonType : byte
{
    KdfChange = 0,
    KeyRotation = 1,
}
