using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

class Program
{
    /**
     * Clase auxiliar que representa un paciente extendido.
     * Contiene información adicional como prioridad y orden de llegada.
     */
    class PacienteExtendido
    {
        public Paciente Paciente { get; set; } = null!;

        /** Prioridad del paciente: 1 = Emergencia, 2 = Urgencia, 3 = Consulta general */
        public int Prioridad { get; set; }

        /** Orden en el que llegó el paciente al hospital */
        public int NumeroLlegada { get; set; }
    }

    /**
     * Método principal que simula la atención de 4 pacientes con diferentes prioridades.
     * Los pacientes se atienden según prioridad y orden de llegada.
     */
    static void Main(string[] args)
    {
        Random random = new Random();

        // Crear pacientes con tiempos y prioridades aleatorios
        List<PacienteExtendido> pacientes = new List<PacienteExtendido>
        {
            new PacienteExtendido { Paciente = new Paciente(0, 0, random.Next(5, 16)), Prioridad = 2 },
            new PacienteExtendido { Paciente = new Paciente(0, 3, random.Next(5, 16)), Prioridad = 1 },
            new PacienteExtendido { Paciente = new Paciente(0, 2, random.Next(5, 16)), Prioridad = 3 },
            new PacienteExtendido { Paciente = new Paciente(0, 1, random.Next(5, 16)), Prioridad = 2 }
        };

        // Asignar número de llegada en función del orden real
        var ordenLlegada = pacientes.OrderBy(p => p.Paciente.LlegadaHospital).ToList();
        for (int i = 0; i < ordenLlegada.Count; i++)
        {
            ordenLlegada[i].NumeroLlegada = i + 1;
        }

        // Ordenar por prioridad y luego por llegada
        var ordenConsulta = ordenLlegada
            .OrderBy(p => p.Prioridad)
            .ThenBy(p => p.Paciente.LlegadaHospital)
            .ToList();

        Console.WriteLine("🩺 Pacientes en orden de atención:\n");

        // Mostrar información de los pacientes en orden de consulta
        foreach (var p in ordenConsulta)
        {
            string prioridadTexto = p.Prioridad == 1 ? "Emergencia" :
                                    p.Prioridad == 2 ? "Urgencia" : "Consulta general";

            Console.WriteLine($"Id: {p.Paciente.Id}, Prioridad: {prioridadTexto}, Número de llegada: {p.NumeroLlegada}, Tiempo Consulta: {p.Paciente.TiempoConsulta}s");
        }

        Console.WriteLine("\n⏳ Simulando consultas...\n");

        // Simular el proceso de consulta de cada paciente
        foreach (var p in ordenConsulta)
        {
            p.Paciente.Estado = 1; // En consulta
            Console.WriteLine($" ➡️ 🩺 Paciente {p.Paciente.Id} ENTRA en consulta por {p.Paciente.TiempoConsulta} segundos...");
            Thread.Sleep(1000); // Simulación rápida
            p.Paciente.Estado = 2; // Finalizado
            Console.WriteLine($"⬅️  Paciente {p.Paciente.Id} ha FINALIZADO la consulta.\n");
        }

        // Identificar al paciente que sale primero (menor tiempo de consulta)
        var primero = ordenConsulta.OrderBy(p => p.Paciente.TiempoConsulta).First();
        Console.WriteLine($"⬅️⬅️⬅️ El primer paciente en salir fue el ID {primero.Paciente.Id} con prioridad {primero.Prioridad} y tiempo de consulta {primero.Paciente.TiempoConsulta}s.");
    }
}