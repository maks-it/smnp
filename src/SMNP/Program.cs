using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpLib.Messaging;

namespace MaksIT.SMNP {
  class Program {
    // Define exit codes
    const int SuccessExitCode = 0;
    const int FileReadErrorExitCode = 1;
    const int ResolveHostErrorExitCode = 2;
    const int SnmpRequestErrorExitCode = 3;

    static void Main(string[] args) {
      try {
        // Read actions from the file
        var actions = File.ReadAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "actions.txt"));
        if (actions.Length == 0) {
          Console.WriteLine("Actions file is empty.");
          Environment.Exit(SuccessExitCode); // No actions to perform, considered success
        }

        foreach (var action in actions) {
          var splitAction = action.Split(" ");
          if (splitAction.Length < 4) {
            Console.WriteLine($"Invalid action format: {action}");
            Environment.ExitCode = FileReadErrorExitCode;
            continue;
          }

          // Define the necessary variables
          string host = splitAction[0];
          string community = splitAction[1];
          string oid = splitAction[2];
          int value;
          if (!Int32.TryParse(splitAction[3], out value)) {
            Console.WriteLine($"Invalid integer value in action: {action}");
            Environment.ExitCode = FileReadErrorExitCode;
            continue;
          }

          // Resolve the hostname to an IP address
          IPAddress targetIp = ResolveHostToIp(host);
          if (targetIp == null) {
            Console.WriteLine($"Could not resolve host: {host}");
            Environment.ExitCode = ResolveHostErrorExitCode;
            continue;
          }

          IPEndPoint target = new IPEndPoint(targetIp, 161);

          // Create an SNMP PDU for setting the value
          List<Variable> variables = new List<Variable>
          {
                        new Variable(new ObjectIdentifier(oid), new Integer32(value))
                    };

          try {
            // Send the SNMP request
            var result = Messenger.Set(VersionCode.V2, target, new OctetString(community), variables, 6000);
            Console.WriteLine($"SNMP request sent successfully to {host}.");
          }
          catch (Exception ex) {
            Console.WriteLine($"Error sending SNMP request to {host}: {ex.Message}");
            Environment.ExitCode = SnmpRequestErrorExitCode;
          }
        }

        // Set success exit code if no errors
        if (Environment.ExitCode == 0) {
          Environment.ExitCode = SuccessExitCode;
        }
      }
      catch (Exception ex) {
        Console.WriteLine($"Error reading actions file: {ex.Message}");
        Environment.Exit(FileReadErrorExitCode);
      }
      finally {
        // Ensure the application exits with the appropriate exit code
        Environment.Exit(Environment.ExitCode);
      }
    }

    static IPAddress ResolveHostToIp(string host) {
      try {
        var hostEntry = Dns.GetHostEntry(host);
        foreach (var address in hostEntry.AddressList) {
          if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) {
            return address;
          }
        }
      }
      catch (Exception ex) {
        Console.WriteLine($"Error resolving host {host}: {ex.Message}");
      }
      return null;
    }
  }
}
