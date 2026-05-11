namespace API_DASM.DTOs
{
    public class CreateFolderRequest
    {
        public string Name { get; set; } = string.Empty;

        public Guid? ParentFolderId { get; set; }

        public Guid CreatedBy { get; set; }
    }
}
