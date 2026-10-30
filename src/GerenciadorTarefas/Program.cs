using System.Text;
using GerenciadorTarefas.Interface;
using GerenciadorTarefas.Servicos;

Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

var caminho = Environment.GetEnvironmentVariable("TAREFAS_ARQUIVO")
    ?? Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".tarefas",
        "tarefas.json");

try
{
    var servico = new ServicoTarefas(new RepositorioJson(caminho));
    new MenuConsole(servico).Executar();
}
catch (InvalidDataException ex)
{
    Console.Error.WriteLine($"Erro: {ex.Message}");
    return 1;
}
catch (IOException ex)
{
    Console.Error.WriteLine($"Erro de arquivo: {ex.Message}");
    return 1;
}

return 0;
