using System;
using System.Collections.Generic;
using System.Threading;

class Program
{
    /**
     * Clase auxiliar para almacenar información detallada del paciente
     * Incluye tiempos de llegada, inicio y fin de consulta.
     */
    class PacienteExtendido
    {
        public Paciente Paciente { get; set; } = null!;

        /** Número de orden de llegada al hospital */
        public int NumeroLlegada { get; set; }

        /** Hora exacta en la que el paciente llega al hospital */
        public DateTime HoraLlegada { get; set; }

        /** Hora en la que el paciente entra a consulta */
        public DateTime HoraInicioConsulta { get; set; }

        /** Hora en la que el paciente finaliza la consulta */
        public DateTime HoraFinConsulta { get; set; }
    }

    /**
     * Punto de entrada principal de la aplicación.
     * Simula 4 pacientes que llegan al hospital y pasan por el proceso de consulta médica.
     */
    static void Main(string[] args)
    {
        Random random = new Random();
        List<PacienteExtendido> pacientes = new List<PacienteExtendido>();

        // Crear 4 pacientes, cada uno llega 2 segundos después del anterior
        for (int i = 0; i < 4; i++)
        {
            int tiempoLlegada = i * 2;
            var paciente = new PacienteExtendido
            {
                Paciente = new Paciente(0, tiempoLlegada, random.Next(5, 16)),
                NumeroLlegada = i + 1
            };
            pacientes.Add(paciente);
        }

        List<Thread> hilos = new List<Thread>();

        // Crear un hilo para cada paciente
        foreach (var p in pacientes)
        {
            Thread hilo = new Thread(() =>
            {
                // Simular el tiempo de llegada
                Thread.Sleep(p.Paciente.LlegadaHospital * 1000);
                p.HoraLlegada = DateTime.Now;
                Console.WriteLine($"🙍 Paciente {p.Paciente.Id} 🏥 Llegado el {p.NumeroLlegada}. 🛑 Estado: Espera.");

                // Simular entrada en consulta
                p.Paciente.Estado = 1;
                p.HoraInicioConsulta = DateTime.Now;
                TimeSpan espera = p.HoraInicioConsulta - p.HoraLlegada;
                Console.WriteLine($"🙍 Paciente {p.Paciente.Id} 🏥 Llegado el {p.NumeroLlegada}. 🩺 Estado: Consulta. ⏱️  Duración: {espera.Seconds} segundos.");

                // Simular tiempo de consulta
                Thread.Sleep(p.Paciente.TiempoConsulta * 1000);

                // Finaliza la consulta
                p.Paciente.Estado = 2;
                p.HoraFinConsulta = DateTime.Now;
                TimeSpan consulta = p.HoraFinConsulta - p.HoraInicioConsulta;
                Console.WriteLine($"🙍 Paciente {p.Paciente.Id} 🏥 Llegado el {p.NumeroLlegada}. 🔚 Estado: Finalizado. ✅ Completado: {consulta.Seconds} segundos.");
            });

            hilo.Start();
            hilos.Add(hilo);
        }

        // Esperar a que todos los hilos terminen
        foreach (var hilo in hilos)
        {
            hilo.Join();
        }

        Console.WriteLine("\n✅ Todos los pacientes han sido atendidos.");
    }
}



