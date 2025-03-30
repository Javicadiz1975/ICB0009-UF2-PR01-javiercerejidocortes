using System;
using System.Threading;

class Program
{
    // Array que representa los 4 médicos disponibles (true = ocupado, false = libre)
    static bool[] medicosOcupados = new bool[4];
    static object lockMedicos = new object(); // Objeto de bloqueo para evitar condiciones de carrera

    /**
     * Método principal que simula la llegada de 4 pacientes.
     * Cada paciente llega con 2 segundos de diferencia y se atiende en un hilo independiente.
     */
    static void Main(string[] args)
    {
        Console.WriteLine("🏥 Inicio de la simulación - Tarea #1");

        // Crear 4 hilos de pacientes que llegan secuencialmente
        for (int i = 0; i < 4; i++)
        {
            int pacienteNumero = i + 1;

            // Crear hilo del paciente
            Thread hiloPaciente = new Thread(() => AtenderPaciente(pacienteNumero));
            hiloPaciente.Start();

            // Espera de 2 segundos entre llegadas
            Thread.Sleep(2000);
        }
    }

    /**
     * Simula el proceso de atención de un paciente.
     * Incluye llegada, espera por médico libre, consulta y salida.
     *
     * @param numeroPaciente Identificador único del paciente
     */
    static void AtenderPaciente(int numeroPaciente)
    {
        Console.WriteLine($"🕑 [Llegada] Paciente {numeroPaciente} ha llegado al hospital.");

        // Esperar hasta encontrar un médico disponible
        int medicoAsignado = EsperarMedicoLibre();

        Console.WriteLine($"➡️  🩺 [Consulta] Paciente {numeroPaciente} entra con el Médico {medicoAsignado + 1}.");

        // Simular consulta de 10 segundos
        Thread.Sleep(10000);

        Console.WriteLine($"⬅️ [Salida] Paciente {numeroPaciente} ha terminado con el Médico {medicoAsignado + 1}.");

        // Liberar médico tras finalizar consulta
        lock (lockMedicos)
        {
            medicosOcupados[medicoAsignado] = false;
        }
    }

    /**
     * Busca un médico disponible y lo marca como ocupado.
     * Si todos los médicos están ocupados, espera hasta que uno se libere.
     *
     * @return Índice del médico asignado (0 a 3)
     */
    static int EsperarMedicoLibre()
    {
        int medico = -1;
        bool asignado = false;

        while (!asignado)
        {
            lock (lockMedicos)
            {
                for (int i = 0; i < medicosOcupados.Length; i++)
                {
                    if (!medicosOcupados[i])
                    {
                        medicosOcupados[i] = true;
                        medico = i;
                        asignado = true;
                        break;
                    }
                }
            }

            // Si no se ha encontrado médico, esperar antes de volver a intentar
            if (!asignado)
            {
                Thread.Sleep(500); // Espera de medio segundo
            }
        }

        return medico;
    }
}

