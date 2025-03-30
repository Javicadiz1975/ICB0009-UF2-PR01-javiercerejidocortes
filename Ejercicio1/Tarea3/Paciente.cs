using System;

public class Paciente
{
    private static Random random = new Random();

    public int Id { get; set; }
    public int LlegadaHospital { get; set; }
    public int TiempoConsulta { get; set; }
    public int Estado { get; set; } // 0: Espera, 1: Consulta, 2: Finalizado

    public Paciente(int id, int llegadaHospital, int tiempoConsulta)
    {
        Id = id == 0 ? random.Next(1, 101) : id;
        LlegadaHospital = llegadaHospital;
        TiempoConsulta = tiempoConsulta;
        Estado = 0;
    }
}
