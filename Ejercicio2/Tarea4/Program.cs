using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

class Program
{
    /**
     * Clase que extiende la información del paciente con su número de llegada
     * y la hora exacta en la que llega al hospital.
     */
    class PacienteExtendido
    {
        public Paciente Paciente { get; set; } = null!;
        public int NumeroLlegada { get; set; }
        public DateTime HoraLlegada { get; set; }
    }

    // Lista compartida donde los pacientes esperan su turno
    static List<PacienteExtendido> colaEspera = new List<PacienteExtendido>();

    // Objeto de bloqueo para sincronización de la cola
    static object lockCola = new object();

    // Semáforo que representa los 4 médicos disponibles
    static Semaphore semaforoMedicos = new Semaphore(4, 4);

    // Cantidad total de pacientes a procesar
    static int pacientesTotales = 20;

    // Contador de pacientes ya atendidos
    static int pacientesProcesados = 0;

    /**
     * Método principal.
     * Simula la llegada secuencial de 20 pacientes y su atención médica
     * por prioridad y orden de llegada.
     */
    static void Main(string[] args)
    {
        Random random = new Random();
        List<Thread> hilosPacientes = new List<Thread>();

        // Generador de pacientes (uno cada 2 segundos)
        for (int i = 0; i < pacientesTotales; i++)
        {
            int tiempoLlegada = i * 2;
            int llegada = i + 1;

            Thread hilo = new Thread(() =>
            {
                Thread.Sleep(tiempoLlegada * 1000); // Simula el tiempo de llegada
                var paciente = new PacienteExtendido
                {
                    Paciente = new Paciente(0, tiempoLlegada, random.Next(5, 16)),
                    NumeroLlegada = llegada,
                    HoraLlegada = DateTime.Now
                };

                lock (lockCola)
                {
                    colaEspera.Add(paciente);
                    Console.WriteLine($"🙍 Paciente {paciente.Paciente.Id} PRIORIDAD {paciente.Paciente.Prioridad} 🏥 ha llegado (#{paciente.NumeroLlegada}). Estado: EsperaConsulta.");
                }
            });

            hilo.Start();
            hilosPacientes.Add(hilo);
        }

        /**
         * Hilo coordinador.
         * Atiende a los pacientes según prioridad y orden de llegada,
         * siempre que haya un médico disponible.
         */
        Thread hiloCoordinador = new Thread(() =>
        {
            while (true)
            {
                PacienteExtendido? siguiente = null;

                lock (lockCola)
                {
                    // Seleccionar el siguiente paciente según prioridad y llegada
                    siguiente = colaEspera
                        .OrderBy(p => p.Paciente.Prioridad)
                        .ThenBy(p => p.NumeroLlegada)
                        .FirstOrDefault();

                    if (siguiente != null)
                        colaEspera.Remove(siguiente);
                }

                if (siguiente != null)
                {
                    semaforoMedicos.WaitOne(); // Esperar médico libre

                    Thread hiloAtencion = new Thread(() =>
                    {
                        siguiente.Paciente.Estado = Estado.Consulta;
                        Console.WriteLine($"🙍 Paciente {siguiente.Paciente.Id} PRIORIDAD {siguiente.Paciente.Prioridad} 🩺 entra a CONSULTA.");

                        Thread.Sleep(siguiente.Paciente.TiempoConsulta * 1000); // Simula consulta

                        siguiente.Paciente.Estado = Estado.Finalizado;
                        Console.WriteLine($"🙍 Paciente {siguiente.Paciente.Id} 🔚 FINALIZA consulta tras {siguiente.Paciente.TiempoConsulta}s.");

                        semaforoMedicos.Release(); // Liberar médico

                        lock (lockCola)
                        {
                            pacientesProcesados++;
                        }
                    });

                    hiloAtencion.Start();
                }
                else
                {
                    Thread.Sleep(200); // Esperar si aún no hay pacientes
                }

                if (pacientesProcesados >= pacientesTotales)
                    break;
            }
        });

        hiloCoordinador.Start();

        // Esperar a que todos los hilos de pacientes terminen
        foreach (var hilo in hilosPacientes)
        {
            hilo.Join();
        }

        hiloCoordinador.Join();

        Console.WriteLine("\n✅ Todos los pacientes han sido atendidos.");
    }
}
