namespace API_DASM.DTOs
{
    public class CreateFolderDto
    {
        public string Name { get; set; }

        public Guid? ParentFolderId { get; set; }

        public Guid CreatedBy { get; set; }
    }
}