using System;
using System.Collections.Generic;
using System.Threading;

class Program
{
    /**
     * Clase auxiliar que contiene datos extendidos sobre cada paciente.
     * Se usa para almacenar tiempos de llegada, consulta y diagnóstico.
     */
    class PacienteExtendido
    {
        public Paciente Paciente { get; set; } = null!;

        /** Número de llegada al hospital (orden de aparición) */
        public int NumeroLlegada { get; set; }

        /** Tiempo real en que llega al hospital */
        public DateTime HoraLlegada { get; set; }

        /** Tiempo real en que entra en consulta */
        public DateTime HoraInicioConsulta { get; set; }

        /** Tiempo real en que finaliza la consulta */
        public DateTime HoraFinConsulta { get; set; }

        /** Tiempo real en que finaliza el diagnóstico (si aplica) */
        public DateTime HoraFinDiagnostico { get; set; }
    }

    /** Semáforo para simular 4 médicos disponibles */
    static Semaphore semaforoMedicos = new Semaphore(4, 4);

    /** Semáforo para simular 2 máquinas de diagnóstico disponibles */
    static Semaphore semaforoDiagnostico = new Semaphore(2, 2);

    /** Variable que define el turno actual de diagnóstico */
    static int turnoDiagnostico = 1;

    /** Objeto de bloqueo para sincronizar el turno de diagnóstico */
    static object lockTurno = new object();

    /**
     * Método principal.
     * Crea 20 pacientes, simula su llegada secuencial y los atiende según disponibilidad de médicos y máquinas.
     */
    static void Main(string[] args)
    {
        Random random = new Random();
        List<PacienteExtendido> pacientes = new List<PacienteExtendido>();
        List<Thread> hilos = new List<Thread>();

        // Crear 20 pacientes con llegada secuencial cada 2 segundos
        for (int i = 0; i < 20; i++)
        {
            int tiempoLlegada = i * 2;
            pacientes.Add(new PacienteExtendido
            {
                Paciente = new Paciente(0, tiempoLlegada, random.Next(5, 16)),
                NumeroLlegada = i + 1
            });
        }

        // Lanzar un hilo por cada paciente
        foreach (var p in pacientes)
        {
            Thread hilo = new Thread(() =>
            {
                // Simular tiempo de llegada
                Thread.Sleep(p.Paciente.LlegadaHospital * 1000);
                p.HoraLlegada = DateTime.Now;
                Console.WriteLine($"🙍 Paciente {p.Paciente.Id}. 🏥 Llegado el {p.NumeroLlegada}. 🛑 Estado: EsperaConsulta.");

                // Esperar médico libre
                semaforoMedicos.WaitOne();

                // Entra en consulta
                p.Paciente.Estado = Estado.Consulta;
                p.HoraInicioConsulta = DateTime.Now;
                TimeSpan espera = p.HoraInicioConsulta - p.HoraLlegada;
                Console.WriteLine($"🙍 Paciente {p.Paciente.Id}. 🏥 Llegado el {p.NumeroLlegada}. 🩺 Estado: Consulta. Duración espera: {espera.Seconds} segundos.");

                Thread.Sleep(p.Paciente.TiempoConsulta * 1000);
                p.HoraFinConsulta = DateTime.Now;

                semaforoMedicos.Release(); // Libera al médico

                // Diagnóstico si se requiere, respetando el turno
                if (p.Paciente.RequiereDiagnostico)
                {
                    p.Paciente.Estado = Estado.EsperaDiagnostico;
                    Console.WriteLine($"🙍 Paciente {p.Paciente.Id}. 🏥 Llegado el {p.NumeroLlegada}. 🧪 Estado: EsperaDiagnostico.");

                    while (true)
                    {
                        lock (lockTurno)
                        {
                            if (turnoDiagnostico == p.NumeroLlegada)
                            {
                                semaforoDiagnostico.WaitOne();
                                Console.WriteLine($"🙍 Paciente {p.Paciente.Id}. 🏥 Llegado el {p.NumeroLlegada}. 🔬 Estado: Diagnóstico iniciado (15s).");
                                Thread.Sleep(15000);
                                p.HoraFinDiagnostico = DateTime.Now;
                                semaforoDiagnostico.Release();

                                turnoDiagnostico++;
                                break;
                            }
                        }
                        Thread.Sleep(100); // Espera activa hasta su turno
                    }
                }
                else
                {
                    // Aunque no haga diagnóstico, avanza el turno
                    lock (lockTurno)
                    {
                        turnoDiagnostico++;
                    }
                }

                // Finaliza atención
                p.Paciente.Estado = Estado.Finalizado;
                string duracion = p.Paciente.RequiereDiagnostico
                    ? $"{(p.HoraFinDiagnostico - p.HoraFinConsulta).Seconds} segundos (diagnóstico)"
                    : $"{(p.HoraFinConsulta - p.HoraInicioConsulta).Seconds} segundos (consulta)";

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
