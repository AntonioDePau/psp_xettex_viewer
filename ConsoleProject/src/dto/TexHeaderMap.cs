namespace ConsoleProject.DTO {

    public class TexHeaderMap {

        public string FileExtension { get; set; }
        public int FileVersion { get; set; }
        public short FileCount { get; set; }
        public short FileCountB { get; set; }
        public int Unk0c { get; set; }
        public int FileListOffset { get; set; }
        public int FileInfoOffset { get; set; }
        public int FileNamesOffset { get; set; }

    }
}
