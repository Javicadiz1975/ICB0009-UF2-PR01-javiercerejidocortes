using System;
using System.Collections.Generic;
using System.Threading;

class Program
{
    /**
     * Clase que representa un paciente extendido con información adicional
     * como tiempos de llegada, consulta y diagnóstico.
     */
    class PacienteExtendido
    {
        public Paciente Paciente { get; set; } = null!;

        /** Número de orden de llegada del paciente */
        public int NumeroLlegada { get; set; }

        /** Hora en la que llega al hospital */
        public DateTime HoraLlegada { get; set; }

        /** Hora en la que entra en consulta */
        public DateTime HoraInicioConsulta { get; set; }

        /** Hora en la que finaliza la consulta */
        public DateTime HoraFinConsulta { get; set; }

        /** Hora en la que finaliza el diagnóstico (si es necesario) */
        public DateTime HoraFinDiagnostico { get; set; }
    }

    /** Semáforo que representa las 2 máquinas de diagnóstico disponibles */
    static Semaphore diagnosticoDisponible = new Semaphore(2, 2);

    /**
     * Punto de entrada principal.
     * Crea 4 pacientes y simula su paso por consulta y diagnóstico (si es necesario).
     */
    static void Main(string[] args)
    {
        Random random = new Random();
        List<PacienteExtendido> pacientes = new List<PacienteExtendido>();

        // Crear 4 pacientes con llegada secuencial cada 2 segundos
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
                // 🕒 Simulación de llegada al hospital
                Thread.Sleep(p.Paciente.LlegadaHospital * 1000);
                p.HoraLlegada = DateTime.Now;
                Console.WriteLine($"🙍 Paciente {p.Paciente.Id}. 🏥 Llegado el {p.NumeroLlegada}. 🛑 Estado: EsperaConsulta.");

                // 🩺 Entra en consulta
                p.Paciente.Estado = Estado.Consulta;
                p.HoraInicioConsulta = DateTime.Now;
                TimeSpan espera = p.HoraInicioConsulta - p.HoraLlegada;
                Console.WriteLine($"🙍 Paciente {p.Paciente.Id}. 🏥 Llegado el {p.NumeroLlegada}. 🩺 Estado: Consulta. Duración: {espera.Seconds} segundos.");

                Thread.Sleep(p.Paciente.TiempoConsulta * 1000); // Simular consulta
                p.HoraFinConsulta = DateTime.Now;

                // 🔬 Diagnóstico si es necesario
                if (p.Paciente.RequiereDiagnostico)
                {
                    p.Paciente.Estado = Estado.EsperaDiagnostico;
                    Console.WriteLine($"🙍 Paciente {p.Paciente.Id}. 🏥 Llegado el {p.NumeroLlegada}. 🧪 Estado: EsperaDiagnostico.");

                    diagnosticoDisponible.WaitOne(); // Espera turno en máquina

                    Console.WriteLine($"🙍 Paciente {p.Paciente.Id}. 🏥 Llegado el {p.NumeroLlegada}. 🔬 Estado: Diagnóstico iniciado (15s).");
                    Thread.Sleep(15000);
                    p.HoraFinDiagnostico = DateTime.Now;

                    diagnosticoDisponible.Release(); // Libera la máquina
                }

                // ✅ Finaliza el proceso
                p.Paciente.Estado = Estado.Finalizado;
                string duracion = p.Paciente.RequiereDiagnostico ?
                    $"{(p.HoraFinDiagnostico - p.HoraFinConsulta).Seconds} segundos (diagnóstico)" :
                    $"{(p.HoraFinConsulta - p.HoraInicioConsulta).Seconds} segundos (consulta)";

                Console.WriteLine($"🙍 Paciente {p.Paciente.Id}. 🏥 Llegado el {p.NumeroLlegada}. 🔚 Estado: Finalizado. Duración: {duracion}.");
            });

            hilo.Start();
            hilos.Add(hilo);
        }

        // Esperar que todos los hilos finalicen
        foreach (var hilo in hilos)
        {
            hilo.Join();
        }

        Console.WriteLine("\n✅ Todos los pacientes han sido atendidos.");
    }
}
