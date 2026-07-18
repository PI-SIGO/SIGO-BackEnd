namespace SIGO.Security
{
    public static class ClienteDataMasking
    {
        public static string MaskDocument(string normalizedDocument)
        {
            if (normalizedDocument.Length < 3)
                return "***";

            return $"***.***.{normalizedDocument[^3..]}-**";
        }

        public static string MaskContact(string normalizedContact)
        {
            var separatorIndex = normalizedContact.IndexOf('@');
            if (separatorIndex <= 0 || separatorIndex == normalizedContact.Length - 1)
            {
                return normalizedContact.Length >= 4
                    ? $"***{normalizedContact[^4..]}"
                    : "***";
            }

            return $"{normalizedContact[0]}***{normalizedContact[separatorIndex..]}";
        }
    }
}
