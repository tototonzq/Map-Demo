using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using Unity.Networking.Transport.Relay;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace SkeldMap
{
    // ตัวจัดการเซสชันออนไลน์ (Relay + Netcode for GameObjects) · NetworkManager สร้างจากโค้ดเท่านั้น ไม่อยู่ใน scene
    public static class NetSession
    {
        public const int MaxJoiners = 9;

        public static string Status = "";      // ข้อความสถานะ (ไทย) ให้ UI แสดง
        public static string Error = "";       // ข้อความผิดพลาด (ว่าง = ไม่มี)
        public static bool Busy;               // กำลังเชื่อมต่ออยู่
        public static string JoinCode = "";    // รหัสห้อง Relay (เฉพาะ host)

        public static bool Active =>
            NetworkManager.Singleton != null && (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsConnectedClient);

        public static bool IsHost => NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;

        public static UnityTransport EnsureNetworkManager()
        {
            if (NetworkManager.Singleton != null) return NetworkManager.Singleton.GetComponent<UnityTransport>();
            var go = new GameObject("NetworkManager");
            var nm = go.AddComponent<NetworkManager>();
            var ut = go.AddComponent<UnityTransport>();
            UnityEngine.Object.DontDestroyOnLoad(go);
            nm.NetworkConfig = new NetworkConfig();
            nm.NetworkConfig.NetworkTransport = ut;
            nm.NetworkConfig.PlayerPrefab = Resources.Load<GameObject>("Net/NetPlayer");
            if (nm.NetworkConfig.PlayerPrefab == null)
                Debug.LogWarning("[Net] ไม่พบ Resources/Net/NetPlayer — รัน Skeld/Create Net Prefab ก่อน");
            return ut;
        }

        public const ushort DefaultPort = 7777;
        public static string LocalIp = "";   // IP ของเครื่องนี้ (host LAN) ให้เพื่อนใส่

        // ───── LAN: ไม่ใช้ UGS/อินเทอร์เน็ตเลย ─────
        public static string GetLocalIPv4()
        {
            try
            {
                foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != OperationalStatus.Up || ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                    foreach (var ua in ni.GetIPProperties().UnicastAddresses)
                        if (ua.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ua.Address)
                            && !ua.Address.ToString().StartsWith("169.254")) return ua.Address.ToString();
                }
                foreach (var a in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
                    if (a.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(a)) return a.ToString();
            }
            catch (Exception) { }
            return "127.0.0.1";
        }

        public static bool HostLan(ushort port = DefaultPort)
        {
            if (Busy) return false;
            Error = ""; JoinCode = "";
            try
            {
                var ut = EnsureNetworkManager();
                ut.SetConnectionData("0.0.0.0", port, "0.0.0.0");
                if (!NetworkManager.Singleton.StartHost()) throw new Exception("เริ่ม Host ไม่สำเร็จ");
                LocalIp = GetLocalIPv4();
                JoinCode = LocalIp + ":" + port;
                Status = "สร้างห้อง LAN สำเร็จ";
                return true;
            }
            catch (Exception ex)
            {
                Error = "สร้างห้อง LAN ไม่สำเร็จ: " + ex.Message;
                Status = ""; Shutdown();
                return false;
            }
        }

        public static async Task JoinLan(string ip, ushort port = DefaultPort)
        {
            if (Busy) return;
            Busy = true; Error = "";
            Status = "กำลังเชื่อมต่อ LAN...";
            try
            {
                ip = (ip ?? "").Trim();
                if (ip.Contains(":") && ushort.TryParse(ip.Substring(ip.LastIndexOf(':') + 1), out var pp)) { port = pp; ip = ip.Substring(0, ip.LastIndexOf(':')); }
                if (ip.Length == 0) throw new Exception("กรุณาใส่ IP ของผู้สร้างห้อง");
                var ut = EnsureNetworkManager();
                ut.SetConnectionData(ip, port);
                if (!NetworkManager.Singleton.StartClient()) throw new Exception("เริ่ม Client ไม่สำเร็จ");
                float t = 0f;
                while (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsConnectedClient && t < 10f)
                {
                    await Task.Delay(100); t += 0.1f;
                }
                if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsConnectedClient) throw new Exception("หมดเวลาเชื่อมต่อ (เช็ก IP/ไฟร์วอลล์/วงเน็ตเวิร์กเดียวกัน)");
                Status = "เชื่อมต่อแล้ว";
            }
            catch (Exception ex)
            {
                Error = "เข้าร่วมห้อง LAN ไม่สำเร็จ: " + ex.Message;
                Status = ""; Shutdown();
            }
            finally { Busy = false; }
        }

        static async Task InitAuthAsync()
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
                await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        static string Describe(string prefix, Exception ex)
        {
            string m = ex.Message ?? "";
            string low = m.ToLowerInvariant();
            if (low.Contains("project id") || low.Contains("not linked") || low.Contains("projectid") || low.Contains("project is not"))
                return "ยังไม่ได้เชื่อมโปรเจกต์กับ Unity Gaming Services — ไปที่ Project Settings → Services เพื่อ Link project และเปิดใช้ Relay";
            return prefix + ": " + m;
        }

        public static async Task<string> HostAsync()
        {
            if (Busy) return null;
            Busy = true; Error = ""; JoinCode = "";
            Status = "กำลังสร้างห้อง...";
            try
            {
                await InitAuthAsync();
                var ut = EnsureNetworkManager();
                Allocation alloc = await RelayService.Instance.CreateAllocationAsync(MaxJoiners);
                string code = await RelayService.Instance.GetJoinCodeAsync(alloc.AllocationId);
                ut.SetRelayServerData(new RelayServerData(alloc, "dtls"));
                if (!NetworkManager.Singleton.StartHost()) throw new Exception("เริ่ม Host ไม่สำเร็จ");
                JoinCode = code;
                Status = "สร้างห้องสำเร็จ";
                return code;
            }
            catch (Exception ex)
            {
                Error = Describe("สร้างห้องไม่สำเร็จ", ex);
                Status = "";
                Debug.LogWarning("[Net] Host failed: " + ex);
                Shutdown();
                return null;
            }
            finally { Busy = false; }
        }

        // เชื่อมต่อ Relay และรอ client เชื่อมต่อจริง · สำเร็จดูที่ Active (ล้มเหลว Error จะมีข้อความ)
        public static async Task JoinAsync(string code)
        {
            if (Busy) return;
            Busy = true; Error = "";
            Status = "กำลังเชื่อมต่อ...";
            try
            {
                code = (code ?? "").Trim().ToUpperInvariant();
                if (code.Length == 0) throw new Exception("กรุณาใส่รหัสห้อง");
                await InitAuthAsync();
                var ut = EnsureNetworkManager();
                JoinAllocation ja = await RelayService.Instance.JoinAllocationAsync(code);
                ut.SetRelayServerData(new RelayServerData(ja, "dtls"));
                if (!NetworkManager.Singleton.StartClient()) throw new Exception("เริ่ม Client ไม่สำเร็จ");
                float t = 0f;
                while (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsConnectedClient && t < 15f)
                {
                    await Task.Delay(100); t += 0.1f;
                }
                if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsConnectedClient) throw new Exception("หมดเวลาเชื่อมต่อ");
                Status = "เชื่อมต่อแล้ว";
            }
            catch (Exception ex)
            {
                Error = Describe("เข้าร่วมห้องไม่สำเร็จ", ex);
                Status = "";
                Debug.LogWarning("[Net] Join failed: " + ex);
                Shutdown();
            }
            finally { Busy = false; }
        }

        public static void Shutdown()
        {
            JoinCode = "";
            var nm = NetworkManager.Singleton;
            if (nm == null) return;
            nm.Shutdown();
            UnityEngine.Object.Destroy(nm.gameObject);
        }
    }
}
