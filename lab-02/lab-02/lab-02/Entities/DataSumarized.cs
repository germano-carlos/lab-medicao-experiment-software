using CsvHelper.Configuration.Attributes;

namespace lab_02.Entities
{
    public class DataSumarized
    {
        [Name("file")]
        public string File {get;set;}
        [Name("class")]
        public string Class {get;set;}
        [Name("type")]
        public string Type {get;set;}
        [Name("cbo")]
        public double Cbo {get;set;}
        [Name("cboModified")]
        public double CboModified {get;set;}
        [Name("dit")]
        public double Dit {get;set;}
        [Name("lcom")]
        public double Lcom {get;set;}
        [Name("lcom*")]
        public double Lcom2 {get;set;}
        [Name("loc")]
        public long Loc {get;set;}
    }
}