using Voluta.Abstractions.Checkpoint;
using Voluta.Abstractions.Diagnostics;

namespace Voluta.Checkpoints.S3.Wire;

/// <summary>
///     S3 checkpoint JSON wire-format versioning (duplicated per provider package).
/// </summary>
internal static class CheckpointWireFormat
{
    /// <summary>Current wire document version written by this package.</summary>
    public const int Version = 1;

    /// <summary>Stable code when a document's version is newer than this package supports.</summary>
    public const string UnsupportedCode = VolutaErrorCodes.CheckpointUnsupportedFormatVersion;

    public static void EnsureSupported(int formatVersion)
    {
        if (formatVersion == Version)
        {
            return;
        }

        throw new CheckpointStoreException(
            UnsupportedCode,
            $"Checkpoint wire format version {formatVersion} is not supported (supported: {Version}).");
    }
}
