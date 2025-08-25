namespace application.model.IPO
{
    // Management team
    public class ManagementTeam
    {
        public string CEO { get; set; }
        public string CFO { get; set; }
        public List<string> BoardMembers { get; set; } = new();
    }
}
