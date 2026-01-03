using Shuttle.Core.Contract;

namespace Shuttle.Hopper;

public static class TransportMessageExtensions
{
    extension(TransportMessage transportMessage)
    {
        public void AcceptInvariants()
        {
            Guard.AgainstNull(transportMessage.MessageId);
            Guard.AgainstEmpty(transportMessage.PrincipalIdentityName);
            Guard.AgainstEmpty(transportMessage.MessageType);
            Guard.AgainstEmpty(transportMessage.AssemblyQualifiedName);
        }

        public bool IsCompressionEnabled()
        {
            return !string.IsNullOrEmpty(transportMessage.CompressionAlgorithm)
                   &&
                   !transportMessage.CompressionAlgorithm.Equals("none", StringComparison.InvariantCultureIgnoreCase);
        }

        public bool HasExpired()
        {
            return transportMessage.ExpiresAt < DateTimeOffset.UtcNow;
        }

        public bool HasExpiryDate()
        {
            return transportMessage.ExpiresAt < DateTimeOffset.MaxValue;
        }

        public bool HasPriority()
        {
            return transportMessage.Priority != 0;
        }

        public bool HasSenderInboxWorkTransportUri()
        {
            return !string.IsNullOrEmpty(transportMessage.SenderInboxWorkTransportUri);
        }

        public bool IsIgnoring()
        {
            return DateTimeOffset.UtcNow < transportMessage.IgnoreUntil;
        }

        public void RegisterFailure(string message)
        {
            transportMessage.RegisterFailure(message, TimeSpan.FromMilliseconds(0));
        }

        public void RegisterFailure(string message, TimeSpan timeSpanToIgnore)
        {
            Guard.AgainstEmpty(message);

            transportMessage.FailureMessages.Add($"[{DateTimeOffset.UtcNow:O}] : {message}");
            transportMessage.IgnoreUntil = DateTimeOffset.UtcNow.Add(timeSpanToIgnore);
        }

        public bool IsEncryptionEnabled()
        {
            return !string.IsNullOrEmpty(transportMessage.EncryptionAlgorithm)
                   &&
                   !transportMessage.EncryptionAlgorithm.Equals("none", StringComparison.InvariantCultureIgnoreCase);
        }

        public void StopIgnoring()
        {
            transportMessage.IgnoreUntil = DateTimeOffset.MinValue;
        }
    }


    extension(IEnumerable<TransportHeader> headers)
    {
        public bool Contains(string key)
        {
            return headers.Any(header => header.Key.Equals(key, StringComparison.InvariantCultureIgnoreCase));
        }
    }

    extension(List<TransportHeader> headers)
    {
        public string GetHeaderValue(string key)
        {
            var header = headers.FirstOrDefault(candidate => candidate.Key.Equals(key, StringComparison.InvariantCultureIgnoreCase));

            return header == null ? string.Empty : header.Value;
        }

        public void Merge(IEnumerable<TransportHeader> headers1)
        {
            foreach (var header in headers1.Where(header => !headers.Contains(header.Key)))
            {
                headers.Add(new()
                {
                    Key = header.Key,
                    Value = header.Value
                });
            }
        }

        public void SetHeaderValue(string key, string value)
        {
            var header = Guard.AgainstNull(headers).FirstOrDefault(candidate => candidate.Key.Equals(key, StringComparison.InvariantCultureIgnoreCase));

            if (header != null)
            {
                header.Value = value;
            }
            else
            {
                headers.Add(new()
                {
                    Key = key,
                    Value = value
                });
            }
        }
    }
}