using System;
using System.Collections.Generic;
using System.Threading;

class Program
{
    /**
     * Clase que almacena datos extendidos de cada paciente.
     * Guarda información como número de llegada y tiempos relevantes.
     */
    class PacienteExtendido
    {
        public Paciente Paciente { get; set; } = null!;

        /** Número de llegada al hospital (1, 2, 3, 4...) */
        public int NumeroLlegada { get; set; }

        /** Hora en la que llega al hospital */
        public DateTime HoraLlegada { get; set; }

        /** Hora en la que entra en consulta */
        public DateTime HoraInicioConsulta { get; set; }

        /** Hora en la que termina la consulta */
        public DateTime HoraFinConsulta { get; set; }

        /** Hora en la que termina el diagnóstico (si se realiza) */
        public DateTime HoraFinDiagnostico { get; set; }
    }

    /** Semáforo para simular las 2 máquinas de diagnóstico */
    static Semaphore diagnosticoDisponible = new Semaphore(2, 2);

    /** Turno actual de acceso a diagnóstico (para forzar orden de llegada) */
    static int turnoDiagnostico = 1;

    /** Bloqueo para sincronizar el turno de diagnóstico */
    static object lockTurno = new object();

    /**
     * Punto de entrada principal. Simula la atención médica de 4 pacientes con posibilidad
     * de diagnóstico adicional en orden estricto de llegada.
     */
    static void Main(string[] args)
    {
        Random random = new Random();
        List<PacienteExtendido> pacientes = new List<PacienteExtendido>();

        // Crear 4 pacientes con llegada cada 2 segundos
        for (int i = 0; i < 4; i++)
        {
            int tiempoLlegada = i * 2;
            pacientes.Add(new PacienteExtendido
            {
                Paciente = new Paciente(0, tiempoLlegada, random.Next(5, 16)),
                NumeroLlegada = i + 1
            });
        }

        List<Thread> hilos = new List<Thread>();

        // Crear un hilo por paciente
        foreach (var p in pacientes)
        {
            Thread hilo = new Thread(() =>
            {
                // 🕒 Simular tiempo hasta llegada al hospital
                Thread.Sleep(p.Paciente.LlegadaHospital * 1000);
                p.HoraLlegada = DateTime.Now;
                Console.WriteLine($"🙍 Paciente {p.Paciente.Id}. 🏥 Llegado el {p.NumeroLlegada}. 🛑 Estado: EsperaConsulta.");

                // 🩺 Entrar en consulta
                p.Paciente.Estado = Estado.Consulta;
                p.HoraInicioConsulta = DateTime.Now;
                TimeSpan espera = p.HoraInicioConsulta - p.HoraLlegada;
                Console.WriteLine($"🙍 Paciente {p.Paciente.Id}. 🏥 Llegado el {p.NumeroLlegada}. 🩺 Estado: Consulta. Duración: {espera.Seconds} segundos.");

                Thread.Sleep(p.Paciente.TiempoConsulta * 1000);
                p.HoraFinConsulta = DateTime.Now;

                // 🔬 Si necesita diagnóstico, seguir orden de llegada
                if (p.Paciente.RequiereDiagnostico)
                {
                    while (true)
                    {
                        lock (lockTurno)
                        {
                            if (turnoDiagnostico == p.NumeroLlegada)
                            {
                                diagnosticoDisponible.WaitOne(); // Esperar máquina libre

                                Console.WriteLine($"🙍 Paciente {p.Paciente.Id}. 🏥 Llegado el {p.NumeroLlegada}. 🔬 Estado: Diagnóstico iniciado (15s).");
                                Thread.Sleep(15000);
                                p.HoraFinDiagnostico = DateTime.Now;

                                diagnosticoDisponible.Release(); // Liberar máquina
                                turnoDiagnostico++;
                                break;
                            }
                        }
                        Thread.Sleep(100); // Espera activa hasta que le toque
                    }
                }
                else
                {
                    // Si no necesita diagnóstico, igual debe avanzar el turno
                    lock (lockTurno)
                    {
                        turnoDiagnostico++;
                    }
                }

                // ✅ Finalizar paciente
                p.Paciente.Estado = Estado.Finalizado;
                string duracion = p.Paciente.RequiereDiagnostico ?
                    $"{(p.HoraFinDiagnostico - p.HoraFinConsulta).Seconds} segundos (diagnóstico)" :
                    $"{(p.HoraFinConsulta - p.HoraInicioConsulta).Seconds} segundos (consulta)";

                Console.WriteLine($"🙍 Paciente {p.Paciente.Id}. 🏥 Llegado el {p.NumeroLlegada}. 🔚 Estado: Finalizado. Duración: {duracion}.");
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
