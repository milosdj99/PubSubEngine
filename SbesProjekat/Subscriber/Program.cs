﻿using AES;
using Common;
using Manager;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Text;
using System.Threading.Tasks;

namespace Subscriber
{
    class Program
    {
        static void Main(string[] args)
        {
            NetTcpBinding binding = new NetTcpBinding();

            ServiceHost host = new ServiceHost(typeof(SubscriberForEngine));

            string address = host.BaseAddresses.First().ToString();
            //string address = "net.tcp://localhost:8888/SubscriberForEngine";
            
            host.AddServiceEndpoint(typeof(ISubscriberForEngine), binding, address);


            ServiceSecurityAuditBehavior newAudit = new ServiceSecurityAuditBehavior();
            newAudit.AuditLogLocation = AuditLogLocation.Application;
            newAudit.ServiceAuthorizationAuditLevel = AuditLevel.SuccessOrFailure;

            host.Description.Behaviors.Remove<ServiceSecurityAuditBehavior>();
            host.Description.Behaviors.Add(newAudit);

            host.Open();


            string srvCertCN = "PubSubEngine";

            NetTcpBinding bindingg = new NetTcpBinding();
            bindingg.Security.Transport.ClientCredentialType = TcpClientCredentialType.Certificate;

            /// Use CertManager class to obtain the certificate based on the "srvCertCN" representing the expected service identity.
            X509Certificate2 srvCert = CertManager.GetCertificateFromStorage(StoreName.TrustedPeople, StoreLocation.LocalMachine, srvCertCN);
            EndpointAddress addresss = new EndpointAddress(new Uri("net.tcp://localhost:9999/Engine"),
                                      new X509CertificateEndpointIdentity(srvCert));

            ClientProxy proxy = new ClientProxy(bindingg, addresss);

            while (true)
            {
                try
                {

                    List<AlarmType> alarmTypes = new List<AlarmType>();
                    int alarmType;

                    Console.WriteLine("Izaberite alarme na koje zelite da se pretplatite.");
                    Console.WriteLine("Kada izaberete sve alarme na koje zelite da se pretplatite unesite 0.\n");
                    do
                    {
                        Console.WriteLine("Unesite tip alarma:\n 1. NO_ALARM\n 2. FALSE_ALARM\n 3. INFO\n 4. WARNING\n 5. ERROR\n");
                        if(!Int32.TryParse(Console.ReadLine(), out alarmType))
                        {
                            Console.WriteLine("Pogresan unos.");
                            continue;
                        }

                        if (alarmType == 0) break;


                        if (alarmType < 1 || alarmType > 5)
                        {
                            Console.WriteLine("Pogresan unos.");
                            continue;
                        }

                        if (alarmTypes.Contains((AlarmType)alarmType - 1))
                        {
                            Console.WriteLine("Vec ste odabrali taj alarm.");
                            continue;
                        }

                        alarmTypes.Add((AlarmType)alarmType - 1);

                    } while (true);
                        


                    string alarmTypess = "";
                    foreach(AlarmType at in alarmTypes)
                    {
                        alarmTypess = alarmTypess + at + " ";
                    }

                    string key = AES.SecretKey.GenerateKey();

                    string startupPath = Path.Combine(Directory.GetParent(System.IO.Directory.GetCurrentDirectory()).Parent.Parent.FullName, "keySubEng.txt");
                    SecretKey.StoreKey(key, startupPath);


                    proxy.Subscribe(AES.Encryption.EncryptString(alarmTypess, key), 
                        AES.Encryption.EncryptString(address, key));

                    

                    Console.WriteLine("Pritisnite x za gasenje:");

                    if (Console.ReadLine() == "x")
                    {
                        break;
                    }

                    
                }
                catch (Exception e)
                {
                    Console.WriteLine("[ERROR] {0}", e.Message);
                    Console.WriteLine("[StackTrace] {0}", e.StackTrace);
                }
                
            }

            proxy.Unsubscribe(address);

        }
    }
}
