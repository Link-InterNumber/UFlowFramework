using System.IO;

namespace PowerCellStudio.Editor
{
    public interface IBundleReferenceBinary
    {
        public void WriteBytes(BinaryWriter writer);

        public void ReadBytes(BinaryReader reader);
    }
}