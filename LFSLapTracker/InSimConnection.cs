using InSimDotNet;
using InSimDotNet.Helpers;
using InSimDotNet.Packets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LFSLapTracker
{
    class InSimConnection
    {
        private static bool s_ConnectToLocal = true;
        private static bool s_TestWithAI = true;

        public void Run()
        {
            while (true)
            {
                m_InSim = new InSim();
                m_InSim.Initialized += OnInitialized;
                m_InSim.Disconnected += OnDisconnected;
                m_InSim.InSimError += OnError;

                Bind<IS_NCN>(OnNewConnection);
                Bind<IS_NPL>(OnNewPlayer);
                Bind<IS_STA>(OnStateInfo);
                Bind<IS_PLP>(OnPlayerPits);
                Bind<IS_PLA>(OnPitLane);
                Bind<IS_RST>(OnRaceStart);
                Bind<IS_LAP>(OnLap);
                Bind<IS_SPX>(OnSplit);
                Bind<IS_NLP>(OnNodeAndLap);
                Bind<IS_SMALL>(OnSmallPacket);

                Console.WriteLine("Attempting to connect...");

                while (!m_InSim.IsConnected)
                {
                    try
                    {
                        string host = s_ConnectToLocal ? "127.0.0.1" : "192.168.0.3";

                        m_InSim.Initialize(new InSimSettings
                        {
                            Host = host,
                            Port = 29999,
                            Admin = string.Empty,
                            Flags = InSimFlags.ISF_NLP | InSimFlags.ISF_LOCAL,
                            Interval = 10,
                        });

                        SendTiny(TinyType.TINY_SST);
                        SendTiny(TinyType.TINY_RST);
                    }
                    catch (Exception)
                    {
                    }

                    Thread.Sleep(1000);
                }

                while (m_InSim.IsConnected)
                {
                    if (m_InGame && !m_InPits)
                    {
                        if (m_RaceTime > m_NextUpdateTime)
                        {
                            if (m_LapDelta < double.MaxValue)
                            {
                                string colour;
                                double delta = m_LapDelta;
                                if (delta < 0.0)
                                {
                                    colour = "^2";
                                }
                                else
                                {
                                    colour = "^1";
                                }
                                SendButton(colour + Utility.ToTimeDeltaString(delta));
                            }
                            else
                            {
                                ClearButtons();
                            }

                            m_NextUpdateTime = m_RaceTime + 1.0;
                        }

                        // Request game time
                        SendTiny(TinyType.TINY_GTH);
                    }

                    Thread.Sleep(10);
                }
            }
        }

        private void Bind<T>(Action<InSim, T> callback) where T : IPacket
        {
            // Keep a reference to callbacks, as InSim will only keep a WeakReference, which may be garbage collected
            if (!m_Callbacks.Contains(callback))
            {
                m_Callbacks.Add(callback);
            }
            m_InSim.Bind<T>(callback);
        }

        private void SendTiny(TinyType type)
        {
            m_InSim.Send(new IS_TINY { SubT = type, ReqI = 1 });
        }

        private void SendMessage(string format, params object[] args)
        {
            string msg = string.Format(format, args);
            m_InSim.Send(new IS_MSL { Msg = "^3LAP TRACKER^7 " + msg });
            Console.WriteLine("LAP TRACKER " + msg);
        }

        private void SendButton(string format, params object[] args)
        {
            m_InSim.Send(new IS_BTN
            {
                ReqI = 1,
                ClickID = 1,
                BStyle = ButtonStyles.ISB_LIGHT,
                L = 75,
                T = 0,
                W = 50,
                H = 10,
                Text = string.Format(format, args)
            });
        }

        private void OnInitialized(object sender, InitializeEventArgs e)
        {
            SendMessage("Connected");
        }

        private void OnDisconnected(object sender, DisconnectedEventArgs e)
        {
            Console.WriteLine("Connection lost ({0})", e.Reason);
        }

        private void OnError(object sender, InSimErrorEventArgs e)
        {
            Console.WriteLine("ERROR: {0}", e.Exception.Message);
        }

        private void OnNewConnection(InSim inSim, IS_NCN packet)
        {
            if (!packet.Remote)
            {
                Console.WriteLine("Found local connection : {0} - {1}", packet.UCID, packet.PName);
                SendMessage("Tracking " + packet.PName);
                m_LocalConnectionId = packet.UCID;
            }
        }

        private void OnNewPlayer(InSim inSim, IS_NPL packet)
        {
            if (packet.UCID == m_LocalConnectionId && (s_TestWithAI || packet.PType == 0 || packet.PType == PlayerTypes.PLT_FEMALE))
            {
                m_LocalPlayerId = packet.PLID;
                m_InPits = ((packet.Flags & PlayerFlags.PIF_INPITS) == PlayerFlags.PIF_INPITS);
                Console.WriteLine("Found local player : {0} - {1}", packet.PLID, m_InPits ? "pit" : "track");

                ResetPlayer();
            }
        }

        private void OnStateInfo(InSim inSim, IS_STA packet)
        {
            bool inGame = ((packet.Flags & StateFlags.ISS_GAME) == StateFlags.ISS_GAME);
            if (inGame && !m_InGame)
            {
                Console.WriteLine("Joined Game");
                SendTiny(TinyType.TINY_NCN);
                SendTiny(TinyType.TINY_NPL);
            }
            else if (!inGame && m_InGame)
            {
                Console.WriteLine("Left Game");
                ClearButtons();
                m_LocalPlayerId = byte.MaxValue;
            }

            if (packet.Track != null)
            {
                // Request full track info
                SendTiny(TinyType.TINY_RST);
            }

            m_InGame = inGame;
        }

        private void OnPlayerPits(InSim inSim, IS_PLP packet)
        {
            if (packet.PLID == m_LocalPlayerId)
            {
                Console.WriteLine("Local player pits");
                m_InPits = true;
                ClearButtons();
            }
        }

        private void OnPitLane(InSim inSim, IS_PLA packet)
        {
            if (packet.PLID == m_LocalPlayerId)
            {
                Console.WriteLine("Pit: {0}", packet.Fact);
            }
        }

        private void OnRaceStart(InSim inSim, IS_RST packet)
        {
            if (packet.ReqI == 0)
            {
                Console.WriteLine("Race start");
                ResetPlayer();

                if (m_BestSplitTimes != null)
                {
                    double time = m_BestSplitTimes.Last();
                    string timeStr = Utility.ToTimeString(time);
                    SendMessage("Best lap: " + timeStr);
                }
            }
            else
            {
                if (m_Track == null || m_Track.ShortName != packet.Track)
                {
                    m_Track = new Track(packet);
                    Console.WriteLine("New track: {0} - {1}", m_Track.ShortName, m_Track.LongName);

                    m_BestSplitTimes = null;
                    m_BestNodeTimes = null;
                }
            }
        }

        private void OnLap(InSim inSim, IS_LAP packet)
        {
            DateTime now = DateTime.UtcNow;
            if (packet.PLID == m_LocalPlayerId)
            {
                double lapTime = packet.LTime.TotalSeconds;
                Console.WriteLine("Lap {0}: {1}", packet.LapsDone, Utility.ToTimeString(lapTime));
                OnSplit(lapTime, m_Track.NumSectors - 1, true);

                if (m_BestSplitTimes == null || lapTime < m_BestSplitTimes.Last())
                {
                    // Beat best lap (or first lap) - promote current split and node times to best
                    m_BestSplitTimes = m_CurrentSplitTimes;
                    m_BestNodeTimes = m_CurrentNodeTimes;

                    string timeStr = Utility.ToTimeString(lapTime);
                    SendMessage("New best lap: " + timeStr);
                }

                // Reset for new lap
                m_CurrentSplitTimes = new double[m_Track.NumSectors];
                m_CurrentNodeTimes = new double[m_Track.NumNodes];
                m_Node = 0;
                m_LapStartTime = m_RaceTime;
            }
        }

        private void OnSplit(InSim inSim, IS_SPX packet)
        {
            if (packet.PLID == m_LocalPlayerId)
            {
                Console.WriteLine("Split {0}: {1}", packet.Split, Utility.ToTimeString(packet.STime));
                OnSplit(packet.STime.TotalSeconds, packet.Split - 1, false);
            }
        }

        private void OnSplit(double time, int split, bool lap)
        {
            m_CurrentSplitTimes[split] = time;
            string timeStr = Utility.ToTimeString(time);

            // Compare to best lap
            if (m_BestSplitTimes == null || m_BestSplitTimes[split] == 0.0)
            {
                SendButton("^7" + timeStr);
            }
            else
            {
                string colour;
                double delta = time - m_BestSplitTimes[split];
                if (delta < 0.0)
                {
                    // Beat best split
                    colour = "^2";
                }
                else
                {
                    // Failed to beat best split
                    colour = "^1";
                }
                SendButton("^7{0}  {1}{2}", timeStr, colour, Utility.ToTimeDeltaString(delta));
            }

            // Show button for 6 seconds
            m_NextUpdateTime = m_RaceTime + 6.0;
        }

        private void OnNodeAndLap(InSim inSim, IS_NLP packet)
        {
            DateTime now = DateTime.Now;
            if (m_InGame && !m_InPits && m_LocalPlayerId != byte.MaxValue)
            {
                foreach (NodeLap nodeLap in packet.Info)
                {
                    if (nodeLap.PLID == m_LocalPlayerId)
                    {
                        // Convert to node index
                        int node = m_Track.GetNodeIndex(nodeLap.Node);

                        if (m_Node == -1 && node < s_MaxNodeJump)
                        {
                            Console.WriteLine("Starting first lap");
                            m_LapStartTime = m_RaceTime;
                        }

                        if (m_Node < node && node < m_Node + s_MaxNodeJump)
                        {
                            m_CurrentNodeTimes[node] = m_RaceTime - m_LapStartTime;
                            m_Node = node;

                            if (m_BestNodeTimes != null)
                            {
                                if (m_CurrentNodeTimes[node] > 0.0 && m_BestNodeTimes[node] > 0.0)
                                {
                                    m_LapDelta = m_CurrentNodeTimes[node] - m_BestNodeTimes[node];
                                }
                                else
                                {
                                    m_LapDelta = double.MaxValue;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void OnSmallPacket(InSim inSim, IS_SMALL packet)
        {
            if (packet.SubT == SmallType.SMALL_RTP)
            {
                m_RaceTime = 0.01 * packet.UVal;
            }
        }

        private void ResetPlayer()
        {
            m_Node = -1;
            m_LapDelta = double.MaxValue;
            if (m_Track != null)
            {
                m_CurrentSplitTimes = new double[m_Track.NumSectors];
                m_CurrentNodeTimes = new double[m_Track.NumNodes];
            }
            ClearButtons();
        }

        private void ClearButtons()
        {
            m_InSim.Send(new IS_BFN
            {
                SubT = ButtonFunction.BFN_CLEAR
            });
        }

        private static int s_MaxNodeJump = 20;

        private InSim m_InSim;
        List<object> m_Callbacks = new List<object>();

        private byte m_LocalConnectionId;
        private byte m_LocalPlayerId;
        private bool m_InGame;
        private bool m_InPits;
        private Track m_Track;

        private int m_Node;

        private double m_RaceTime;
        private double m_LapStartTime;
        private double m_LapDelta;

        private double m_NextUpdateTime;

        private double[] m_BestNodeTimes;
        private double[] m_CurrentNodeTimes;

        private double[] m_BestSplitTimes;
        private double[] m_CurrentSplitTimes;
    }
}
