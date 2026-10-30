using GerenciadorTarefas.Modelos;

namespace GerenciadorTarefas.Servicos;

public class TarefaNaoEncontradaException : Exception
{
    public TarefaNaoEncontradaException(int id)
        : base($"Não existe tarefa com id {id}.")
    {
    }
}

public record FiltroTarefas(
    bool? Concluida = null,
    Prioridade? Nivel = null,
    string? Etiqueta = null,
    bool ApenasAtrasadas = false,
    string? Texto = null);

public record Estatisticas(int Total, int Concluidas, int Atrasadas)
{
    public int Pendentes => Total - Concluidas;

    public double PercentualConcluido => Total == 0 ? 0 : Concluidas * 100.0 / Total;
}

public class ServicoTarefas
{
    private const int TamanhoMaximoTitulo = 120;

    private readonly IRepositorioTarefas _repositorio;
    private readonly Func<DateTime> _agora;
    private readonly List<Tarefa> _tarefas;

    public ServicoTarefas(IRepositorioTarefas repositorio, Func<DateTime>? agora = null)
    {
        _repositorio = repositorio;
        _agora = agora ?? (() => DateTime.Now);
        _tarefas = repositorio.Carregar();
    }

    public Tarefa Adicionar(
        string titulo,
        Prioridade prioridade = Prioridade.Media,
        DateTime? prazo = null,
        string descricao = "",
        IEnumerable<string>? etiquetas = null)
    {
        titulo = ValidarTitulo(titulo);
        ValidarPrazo(prazo);

        var tarefa = new Tarefa
        {
            Id = ProximoId(),
            Titulo = titulo,
            Descricao = descricao.Trim(),
            Prioridade = prioridade,
            Prazo = prazo?.Date,
            CriadaEm = _agora(),
            Etiquetas = NormalizarEtiquetas(etiquetas)
        };

        _tarefas.Add(tarefa);
        Persistir();
        return tarefa;
    }

    public Tarefa Editar(int id, string? titulo = null, Prioridade? prioridade = null, DateTime? prazo = null)
    {
        var tarefa = Obter(id);
        var novoTitulo = titulo is null ? tarefa.Titulo : ValidarTitulo(titulo);
        ValidarPrazo(prazo);

        tarefa.Titulo = novoTitulo;
        if (prioridade.HasValue)
        {
            tarefa.Prioridade = prioridade.Value;
        }

        if (prazo.HasValue)
        {
            tarefa.Prazo = prazo.Value.Date;
        }

        Persistir();
        return tarefa;
    }

    public Tarefa Concluir(int id)
    {
        var tarefa = Obter(id);
        if (tarefa.Concluida)
        {
            throw new InvalidOperationException("A tarefa já está concluída.");
        }

        tarefa.Concluida = true;
        tarefa.ConcluidaEm = _agora();
        Persistir();
        return tarefa;
    }

    public Tarefa Reabrir(int id)
    {
        var tarefa = Obter(id);
        if (!tarefa.Concluida)
        {
            throw new InvalidOperationException("A tarefa não está concluída.");
        }

        tarefa.Concluida = false;
        tarefa.ConcluidaEm = null;
        Persistir();
        return tarefa;
    }

    public void Remover(int id)
    {
        var tarefa = Obter(id);
        _tarefas.Remove(tarefa);
        Persistir();
    }

    public bool EstaAtrasada(Tarefa tarefa)
    {
        return tarefa.Atrasada(_agora());
    }

    public IReadOnlyList<Tarefa> Listar(FiltroTarefas? filtro = null)
    {
        var agora = _agora();
        IEnumerable<Tarefa> consulta = _tarefas;

        if (filtro is not null)
        {
            if (filtro.Concluida.HasValue)
            {
                consulta = consulta.Where(t => t.Concluida == filtro.Concluida.Value);
            }

            if (filtro.Nivel.HasValue)
            {
                consulta = consulta.Where(t => t.Prioridade == filtro.Nivel.Value);
            }

            if (!string.IsNullOrWhiteSpace(filtro.Etiqueta))
            {
                var etiqueta = filtro.Etiqueta.Trim().ToLowerInvariant();
                consulta = consulta.Where(t => t.Etiquetas.Contains(etiqueta));
            }

            if (filtro.ApenasAtrasadas)
            {
                consulta = consulta.Where(t => t.Atrasada(agora));
            }

            if (!string.IsNullOrWhiteSpace(filtro.Texto))
            {
                var texto = filtro.Texto.Trim();
                consulta = consulta.Where(t =>
                    t.Titulo.Contains(texto, StringComparison.OrdinalIgnoreCase) ||
                    t.Descricao.Contains(texto, StringComparison.OrdinalIgnoreCase));
            }
        }

        return consulta
            .OrderBy(t => t.Concluida)
            .ThenByDescending(t => t.Atrasada(agora))
            .ThenByDescending(t => t.Prioridade)
            .ThenBy(t => t.Prazo ?? DateTime.MaxValue)
            .ThenBy(t => t.Id)
            .ToList();
    }

    public Estatisticas ObterEstatisticas()
    {
        var agora = _agora();
        return new Estatisticas(
            _tarefas.Count,
            _tarefas.Count(t => t.Concluida),
            _tarefas.Count(t => t.Atrasada(agora)));
    }

    private Tarefa Obter(int id)
    {
        return _tarefas.FirstOrDefault(t => t.Id == id) ?? throw new TarefaNaoEncontradaException(id);
    }

    private int ProximoId()
    {
        return _tarefas.Count == 0 ? 1 : _tarefas.Max(t => t.Id) + 1;
    }

    private void Persistir()
    {
        _repositorio.Salvar(_tarefas);
    }

    private void ValidarPrazo(DateTime? prazo)
    {
        if (prazo.HasValue && prazo.Value.Date < _agora().Date)
        {
            throw new ArgumentException("O prazo não pode estar no passado.", nameof(prazo));
        }
    }

    private static string ValidarTitulo(string titulo)
    {
        if (string.IsNullOrWhiteSpace(titulo))
        {
            throw new ArgumentException("O título não pode ser vazio.", nameof(titulo));
        }

        titulo = titulo.Trim();
        if (titulo.Length > TamanhoMaximoTitulo)
        {
            throw new ArgumentException(
                $"O título deve ter no máximo {TamanhoMaximoTitulo} caracteres.", nameof(titulo));
        }

        return titulo;
    }

    private static List<string> NormalizarEtiquetas(IEnumerable<string>? etiquetas)
    {
        if (etiquetas is null)
        {
            return new List<string>();
        }

        return etiquetas
            .Select(e => e.Trim().ToLowerInvariant())
            .Where(e => e.Length > 0)
            .Distinct()
            .ToList();
    }
}
