namespace Reec.Inspection.Entities
{
    public sealed class LogDb  
    {
        public int IdLogDB { get; set; }
        public int ErrorNumber { get; set; }
        public int ErrorSeverity { get; set; }
        public int ErrorState { get; set; }
        public string ErrorProcedure { get; set; }
        public int ErrorLine { get; set; }
        public string ErrorMessage { get; set; }
        public DateOnly? CreateDateOnly { get; set; }
        public DateTime CreateDate { get; set; }
    }
}
