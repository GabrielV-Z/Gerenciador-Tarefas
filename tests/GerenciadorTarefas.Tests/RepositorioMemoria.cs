using GerenciadorTarefas.Modelos;
using GerenciadorTarefas.Servicos;

namespace GerenciadorTarefas.Tests;

public class RepositorioMemoria : IRepositorioTarefas
{
    private List<Tarefa> _armazenadas = new();

    public List<Tarefa> Carregar()
    {
        return _armazenadas.Select(Clonar).ToList();
    }

    public void Salvar(IEnumerable<Tarefa> tarefas)
    {
        _armazenadas = tarefas.Select(Clonar).ToList();
    }

    private static Tarefa Clonar(Tarefa t)
    {
        return new Tarefa
        {
            Id = t.Id,
            Titulo = t.Titulo,
            Descricao = t.Descricao,
            Prioridade = t.Prioridade,
            Prazo = t.Prazo,
            Concluida = t.Concluida,
            CriadaEm = t.CriadaEm,
            ConcluidaEm = t.ConcluidaEm,
            Etiquetas = new List<string>(t.Etiquetas)
        };
    }
}
