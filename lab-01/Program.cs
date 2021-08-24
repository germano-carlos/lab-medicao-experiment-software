using System;
namespace lab_01
{
    public class Project
    {
        public string CustomerName { get; set; }
        public string Title { get; set; }
        public DateTime Deadline { get; set; }
    }
    
    class Program
    {
        public static void Main(string[] args)
        {
            Run();
            Console.ReadKey();
        }

        public static void Run()
        {
            //Busca um total de 1000 repositorios, a cada 100x
            Lab01BL.BuscaRepositoriosPaginados(100, 1000);
        }
    }
}
