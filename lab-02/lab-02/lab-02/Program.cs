using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace lab_02
{
    internal static class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Bem vindo ao projeto de medição - LAB 02");
            Console.WriteLine("Iremos inicialmente buscar os repositórios atualizados, caso não queira e já possua o arquivo csv, digite 0");
            Console.WriteLine("Aperte enter para prosseguir em caso de atualização de repositórios");
            string op = Console.ReadLine();

            if (op != "0")
                Debug.WriteLine(JsonConvert.SerializeObject(Lab02BL.BuscaRepositoriosPaginados(50, 1000), Formatting.Indented));
            await Lab02BL.Sumarizacao(null, null, true);
        }
    }
}