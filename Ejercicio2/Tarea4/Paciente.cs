using System;

public class Paciente
{
    private static Random random = new Random();

    public int Id { get; set; }
    public int LlegadaHospital { get; set; }
    public int TiempoConsulta { get; set; }
    public Estado Estado { get; set; }
    public bool RequiereDiagnostico { get; set; }
    public int Prioridad { get; set; } // 1: Emergencia, 2: Urgencia, 3: Consulta general

    public Paciente(int id, int llegadaHospital, int tiempoConsulta)
    {
        Id = id == 0 ? random.Next(1, 101) : id;
        LlegadaHospital = llegadaHospital;
        TiempoConsulta = tiempoConsulta;
        Estado = Estado.EsperaConsulta;
        RequiereDiagnostico = random.Next(2) == 0;
        Prioridad = random.Next(1, 4); // 1 a 3
    }
}
