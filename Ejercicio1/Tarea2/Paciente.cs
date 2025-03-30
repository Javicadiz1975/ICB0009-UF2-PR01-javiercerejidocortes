using System;

public class Paciente
{
    private static Random random = new Random();

    public int Id { get; set; }
    public int LlegadaHospital { get; set; }
    public int TiempoConsulta { get; set; }
    public int Estado { get; set; } // 0 = Espera, 1 = Consulta, 2 = Finalizado

    public Paciente(int Id, int LlegadaHospital, int TiempoConsulta)
    {
        // Si el Id es 0, se genera aleatoriamente
        this.Id = Id == 0 ? random.Next(1, 101) : Id;
        this.LlegadaHospital = LlegadaHospital;
        this.TiempoConsulta = TiempoConsulta;
        this.Estado = 0; // Estado inicial: Espera
    }
}