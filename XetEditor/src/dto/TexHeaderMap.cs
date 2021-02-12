namespace XetEditor.DTO {

    public class TexHeaderMap {

        public string FileExtension { get; set; }
        public int FileVersion { get; set; }
        public short FileCount { get; set; }
        public short FileCountB { get; set; }
        public int Unk0c { get; set; }
        public int FileListOffset { get; set; }
        public int FileInfoOffset { get; set; }
        public int FileNamesOffset { get; set; }

        public TexHeaderMap() {
            // default constructor
        }

        public TexHeaderMap(short count) {
            this.FileExtension = "XET.";
            this.FileVersion = 1;
            this.FileCount = count;
            this.FileCountB = count;
            this.Unk0c = 0;
        }

    }
}
