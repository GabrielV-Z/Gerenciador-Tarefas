namespace GerenciadorTarefas.Modelos;

public class Tarefa
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public Prioridade Prioridade { get; set; } = Prioridade.Media;
    public DateTime? Prazo { get; set; }
    public bool Concluida { get; set; }
    public DateTime CriadaEm { get; set; }
    public DateTime? ConcluidaEm { get; set; }
    public List<string> Etiquetas { get; set; } = new();

    public bool Atrasada(DateTime agora)
    {
        return !Concluida && Prazo.HasValue && Prazo.Value.Date < agora.Date;
    }
}
