using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace lab_01
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("pt-BR");
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("pt-BR");
            
            //Caso queira iniciar um novo arquivo CSV, remover este comentário !
            /*Run();*/

            await Lab01BL.SumarizacaoFinal();

            Console.ReadKey();
        }

        public static void Run()
        {
            //Busca um total de 1000 repositorios, a cada 100x
            Lab01BL.BuscaRepositoriosPaginados(50, 1000);
        }
    }
}
