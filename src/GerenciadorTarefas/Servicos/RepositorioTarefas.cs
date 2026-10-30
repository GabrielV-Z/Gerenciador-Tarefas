using System.Text.Json;
using System.Text.Json.Serialization;
using GerenciadorTarefas.Modelos;

namespace GerenciadorTarefas.Servicos;

public interface IRepositorioTarefas
{
    List<Tarefa> Carregar();
    void Salvar(IEnumerable<Tarefa> tarefas);
}

public class RepositorioJson : IRepositorioTarefas
{
    private static readonly JsonSerializerOptions Opcoes = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _caminho;

    public RepositorioJson(string caminho)
    {
        _caminho = caminho;
    }

    public List<Tarefa> Carregar()
    {
        if (!File.Exists(_caminho))
        {
            return new List<Tarefa>();
        }

        var json = File.ReadAllText(_caminho);
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<Tarefa>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<Tarefa>>(json, Opcoes) ?? new List<Tarefa>();
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException(
                $"O arquivo {_caminho} está corrompido ou em formato inválido.", ex);
        }
    }

    public void Salvar(IEnumerable<Tarefa> tarefas)
    {
        var diretorio = Path.GetDirectoryName(_caminho);
        if (!string.IsNullOrEmpty(diretorio))
        {
            Directory.CreateDirectory(diretorio);
        }

        var temporario = _caminho + ".tmp";
        File.WriteAllText(temporario, JsonSerializer.Serialize(tarefas, Opcoes));
        File.Move(temporario, _caminho, overwrite: true);
    }
}
