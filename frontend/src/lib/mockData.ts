import type {
  AdminOverview,
  Certification,
  DashboardSummary,
  ExamAttempt,
  MaintenanceWindow,
  Question,
} from "../types";

export const mockCertifications: Certification[] = [
  {
    id: "cert-aplus-core1",
    code: "220-1201",
    name: "CompTIA A+ Core 1",
    version: "Core Series 1200",
    description: "Mobile devices, networking, hardware, virtualization and cloud, and troubleshooting.",
    topicCount: 6,
    questionCount: 420,
  },
  {
    id: "cert-aplus-core2",
    code: "220-1202",
    name: "CompTIA A+ Core 2",
    version: "Core Series 1200",
    description: "Operating systems, security, software troubleshooting, and operational procedures.",
    topicCount: 5,
    questionCount: 380,
  },
  {
    id: "cert-networkplus",
    code: "N10-009",
    name: "CompTIA Network+",
    version: "N10-009",
    description: "Networking concepts, infrastructure, network operations, security, and troubleshooting.",
    topicCount: 5,
    questionCount: 410,
  },
];

export const mockDashboard: DashboardSummary = {
  studyStreakDays: 12,
  questionsAnswered: 842,
  averageScore: 76,
  passRate: 68,
  studyTimeMinutesThisWeek: 305,
  leaderboardPosition: 47,
  examReadinessScore: 71,
  weakTopics: [
    { topic: "Subnetting", masteryPercent: 41 },
    { topic: "Cloud Concepts", masteryPercent: 52 },
    { topic: "Wireless Standards", masteryPercent: 58 },
  ],
  strongTopics: [
    { topic: "Hardware Installation", masteryPercent: 92 },
    { topic: "Operating Systems", masteryPercent: 88 },
    { topic: "Mobile Devices", masteryPercent: 85 },
  ],
  recentAttempts: [
    {
      id: "attempt-1",
      certificationId: "cert-aplus-core1",
      mode: "MockExam",
      startedAt: "2026-07-27T09:00:00Z",
      completedAt: "2026-07-27T10:30:00Z",
      durationSeconds: 5400,
      score: 78,
      passed: true,
      totalQuestions: 90,
      correctCount: 70,
    },
    {
      id: "attempt-2",
      certificationId: "cert-networkplus",
      mode: "Practice",
      startedAt: "2026-07-26T18:00:00Z",
      completedAt: "2026-07-26T18:25:00Z",
      durationSeconds: 1500,
      score: 64,
      passed: false,
      totalQuestions: 25,
      correctCount: 16,
    },
  ],
  dailyActivity: [
    { date: "Mon", minutes: 35 },
    { date: "Tue", minutes: 50 },
    { date: "Wed", minutes: 20 },
    { date: "Thu", minutes: 60 },
    { date: "Fri", minutes: 45 },
    { date: "Sat", minutes: 70 },
    { date: "Sun", minutes: 25 },
  ],
};

function buildQuestion(
  idSuffix: string,
  certificationId: string,
  topic: string,
  subtopic: string | undefined,
  difficulty: Question["difficulty"],
  prompt: string,
  explanation: string,
  reference: string,
  options: [string, boolean][]
): Question {
  return {
    id: `q-${idSuffix}`,
    certificationId,
    topic,
    subtopic,
    difficulty,
    prompt,
    explanation,
    reference,
    status: "Published",
    options: options.map(([text, isCorrect], i) => ({
      id: `q-${idSuffix}-o${i + 1}`,
      text,
      isCorrect,
    })),
  };
}

const CERT_APLUS_CORE1 = "cert-aplus-core1";
const CERT_APLUS_CORE2 = "cert-aplus-core2";
const CERT_NETWORKPLUS = "cert-networkplus";

export const mockQuestions: Question[] = [
  // --- CompTIA A+ Core 1 ---
  buildQuestion("101", CERT_APLUS_CORE1, "Mobile Devices", "Laptop Hardware", "Easy",
    "Which laptop component is most commonly replaced by a user to increase available memory without professional repair tools?",
    "SO-DIMM memory modules are designed for easy end-user access on most laptops, unlike soldered components such as the CPU.",
    "A+ Core 1 — Mobile Devices",
    [["SO-DIMM RAM module", true], ["CPU", false], ["Soldered eMMC storage", false], ["Digitizer", false]]),
  buildQuestion("102", CERT_APLUS_CORE1, "Mobile Devices", "Connectivity", "Medium",
    "A user reports their phone will not pair with a new car stereo, but pairs fine with other devices. What is the most likely first troubleshooting step?",
    "Removing (forgetting) the previously paired connection and re-pairing resolves most Bluetooth pairing conflicts caused by stale cached credentials.",
    "A+ Core 1 — Mobile Device Connectivity",
    [["Forget the device on both ends and re-pair", true], ["Factory reset the phone", false], ["Replace the phone's battery", false], ["Update the car's entire infotainment firmware", false]]),
  buildQuestion("103", CERT_APLUS_CORE1, "Networking", "Cabling", "Easy",
    "Which cable category is the minimum typically required to reliably support 10 Gbps Ethernet over short distances?",
    "Cat 6a is rated for 10GBASE-T at full 100-meter distances; Cat 6 can support 10 Gbps only over shorter, limited runs.",
    "A+ Core 1 — Networking",
    [["Cat 5e", false], ["Cat 6a", true], ["Cat 3", false], ["RG-6 coaxial", false]]),
  buildQuestion("104", CERT_APLUS_CORE1, "Networking", "Wireless Standards", "Medium",
    "Which wireless standard introduced OFDMA to improve efficiency in dense, high-user environments?",
    "802.11ax (Wi-Fi 6) introduced OFDMA, allowing an access point to serve multiple clients simultaneously on subdivided channels.",
    "A+ Core 1 — Networking",
    [["802.11n", false], ["802.11ac", false], ["802.11ax", true], ["802.11g", false]]),
  buildQuestion("105", CERT_APLUS_CORE1, "Hardware", "Storage Devices", "Easy",
    "Which storage interface offers the highest theoretical throughput for a consumer desktop today?",
    "NVMe drives running over PCIe lanes offer far higher bandwidth than SATA, USB 2.0, or eSATA interfaces.",
    "A+ Core 1 — Hardware",
    [["SATA III", false], ["NVMe over PCIe", true], ["USB 2.0", false], ["eSATA", false]]),
  buildQuestion("106", CERT_APLUS_CORE1, "Hardware", "Power Supplies", "Medium",
    "A technician is selecting a power supply for a workstation with a high-end GPU. Which rating is most directly relevant to whether the PSU can support that GPU?",
    "Continuous wattage output on the +12V rail(s) determines whether a PSU can reliably feed a power-hungry GPU; the 80 Plus badge only measures efficiency, not capacity.",
    "A+ Core 1 — Hardware",
    [["80 Plus certification tier", false], ["Continuous wattage on the +12V rail", true], ["Case form factor", false], ["Number of SATA power connectors", false]]),
  buildQuestion("107", CERT_APLUS_CORE1, "Virtualization and Cloud", "Cloud Concepts", "Medium",
    "A company wants to rent virtual machines and manage the OS and applications themselves, while the provider manages the physical hardware. Which cloud model is this?",
    "Infrastructure as a Service (IaaS) provides raw compute, storage, and networking; the customer manages the OS, runtime, and applications.",
    "A+ Core 1 — Virtualization and Cloud Computing",
    [["SaaS", false], ["PaaS", false], ["IaaS", true], ["DaaS", false]]),
  buildQuestion("108", CERT_APLUS_CORE1, "Virtualization and Cloud", "Virtualization", "Hard",
    "Which type of hypervisor runs directly on physical hardware without a host operating system, and is typically used in enterprise datacenters?",
    "A Type 1 (bare-metal) hypervisor runs directly on hardware, offering better performance and security isolation than a Type 2 hypervisor hosted inside a general-purpose OS.",
    "A+ Core 1 — Virtualization and Cloud Computing",
    [["Type 2 hypervisor", false], ["Type 1 hypervisor", true], ["Container runtime", false], ["Emulator", false]]),
  buildQuestion("109", CERT_APLUS_CORE1, "Hardware and Network Troubleshooting", "Diagnostics", "Medium",
    "A desktop powers on, fans spin, but there is no video output and no POST beep. What is the most efficient next troubleshooting step?",
    "Reseating RAM (and testing one stick at a time in known-good slots) is a fast, low-risk step that resolves a large share of no-POST issues before moving to more invasive steps.",
    "A+ Core 1 — Troubleshooting",
    [["Reseat the RAM modules", true], ["Replace the power supply immediately", false], ["Reinstall the operating system", false], ["Replace the case fans", false]]),
  buildQuestion("110", CERT_APLUS_CORE1, "Hardware and Network Troubleshooting", "Printers", "Easy",
    "A laser printer produces pages with a repeating vertical smear at a consistent interval down the page. What component is most likely at fault?",
    "A repeating defect at a fixed interval strongly indicates a damaged or contaminated drum, since the defect repeats every one drum rotation.",
    "A+ Core 1 — Troubleshooting",
    [["Damaged imaging drum", true], ["Empty toner cartridge", false], ["Faulty USB cable", false], ["Incorrect paper size setting", false]]),

  // --- CompTIA A+ Core 2 ---
  buildQuestion("201", CERT_APLUS_CORE2, "Operating Systems", "Windows", "Easy",
    "Which built-in Windows tool would an administrator use to view detailed hardware and driver information for troubleshooting?",
    "Device Manager lists all detected hardware, driver versions, and status codes, making it the standard first stop for hardware/driver issues.",
    "A+ Core 2 — Operating Systems",
    [["Device Manager", true], ["Task Scheduler", false], ["Disk Cleanup", false], ["Character Map", false]]),
  buildQuestion("202", CERT_APLUS_CORE2, "Operating Systems", "Command Line", "Medium",
    "Which Windows command-line utility is used to display and modify the IP configuration of network adapters?",
    "ipconfig displays current TCP/IP configuration; /release and /renew flags force a new DHCP lease.",
    "A+ Core 2 — Operating Systems",
    [["ipconfig", true], ["chkdsk", false], ["sfc", false], ["diskpart", false]]),
  buildQuestion("203", CERT_APLUS_CORE2, "Security", "Authentication", "Medium",
    "Which authentication factor category does a fingerprint scan belong to?",
    "A fingerprint is 'something you are' — an inherence factor — distinct from knowledge factors (passwords) or possession factors (security tokens).",
    "A+ Core 2 — Security",
    [["Something you know", false], ["Something you have", false], ["Something you are", true], ["Somewhere you are", false]]),
  buildQuestion("204", CERT_APLUS_CORE2, "Security", "Malware", "Hard",
    "A user's files have all been renamed with an unfamiliar extension and a message demands payment for a decryption key. What type of malware is this?",
    "This is the defining behavior of ransomware: encrypting victim files and demanding payment for the decryption key.",
    "A+ Core 2 — Security",
    [["Ransomware", true], ["Trojan", false], ["Rootkit", false], ["Adware", false]]),
  buildQuestion("205", CERT_APLUS_CORE2, "Security", "Physical Security", "Easy",
    "Which physical security control specifically prevents an unauthorized person from following an authorized person through a secure door?",
    "A mantrap (or access-controlled vestibule) physically prevents tailgating by only allowing one verified person through at a time.",
    "A+ Core 2 — Security",
    [["Mantrap", true], ["Privacy screen", false], ["Cable lock", false], ["Badge reader alone", false]]),
  buildQuestion("206", CERT_APLUS_CORE2, "Software Troubleshooting", "Windows Issues", "Medium",
    "A Windows PC displays a 'Blue Screen of Death' referencing a driver file. What is a reasonable first remediation step?",
    "Booting into Safe Mode and rolling back or removing the recently updated driver is the standard first response to a driver-related BSOD.",
    "A+ Core 2 — Software Troubleshooting",
    [["Boot into Safe Mode and roll back the driver", true], ["Immediately reinstall Windows", false], ["Replace the motherboard", false], ["Disable the network adapter", false]]),
  buildQuestion("207", CERT_APLUS_CORE2, "Software Troubleshooting", "Mobile OS", "Easy",
    "A mobile device is running noticeably slower than usual and several unfamiliar apps have appeared. What should be checked first?",
    "Reviewing recently installed apps and permissions is the fastest way to identify unauthorized or malicious software causing the slowdown.",
    "A+ Core 2 — Software Troubleshooting",
    [["Recently installed apps and their permissions", true], ["Screen brightness setting", false], ["Wallpaper resolution", false], ["Bluetooth pairing history", false]]),
  buildQuestion("208", CERT_APLUS_CORE2, "Operational Procedures", "Documentation", "Medium",
    "Before making a significant configuration change to a production server, what should a technician create or follow?",
    "A documented change management process (with a rollback plan) reduces risk and ensures changes are tracked and reversible.",
    "A+ Core 2 — Operational Procedures",
    [["A change management request with a rollback plan", true], ["A verbal agreement with a coworker", false], ["No documentation is needed for small changes", false], ["A social media post announcing the change", false]]),
  buildQuestion("209", CERT_APLUS_CORE2, "Operational Procedures", "Safety", "Easy",
    "What is the primary purpose of an ESD strap when working inside a computer case?",
    "An ESD strap grounds the technician to prevent static discharge from damaging sensitive electronic components.",
    "A+ Core 2 — Operational Procedures",
    [["To prevent electrostatic discharge from damaging components", true], ["To hold cables out of the way", false], ["To measure voltage", false], ["To ground the power supply to the case", false]]),
  buildQuestion("210", CERT_APLUS_CORE2, "Operational Procedures", "Backup Strategy", "Medium",
    "Which backup strategy stores backups at a physically separate location from the primary site to protect against site-wide disasters?",
    "Off-site backups protect against fire, flood, theft, or other events that could destroy both the production data and a local backup simultaneously.",
    "A+ Core 2 — Operational Procedures",
    [["Off-site backup", true], ["Local incremental backup only", false], ["RAID 0 mirroring", false], ["Snapshot on the same disk", false]]),

  // --- CompTIA Network+ ---
  buildQuestion("301", CERT_NETWORKPLUS, "Networking Concepts", "IP Addressing", "Medium",
    "A technician needs to subnet a /24 network into 4 equal subnets. What subnet mask should be used?",
    "Splitting a /24 into 4 equal subnets requires borrowing 2 bits, giving a /26 mask (255.255.255.192), which yields 4 subnets of 64 addresses each.",
    "Network+ — Objective 1.4",
    [["255.255.255.128", false], ["255.255.255.192", true], ["255.255.255.224", false], ["255.255.255.240", false]]),
  buildQuestion("302", CERT_NETWORKPLUS, "Networking Concepts", "OSI Model", "Easy",
    "At which OSI layer does a standard Ethernet switch primarily operate when forwarding frames based on MAC address?",
    "Switches forward traffic based on MAC addresses, which are examined and used at Layer 2 (the Data Link layer).",
    "Network+ — Objective 1.1",
    [["Layer 1 — Physical", false], ["Layer 2 — Data Link", true], ["Layer 3 — Network", false], ["Layer 4 — Transport", false]]),
  buildQuestion("303", CERT_NETWORKPLUS, "Networking Concepts", "Ports and Protocols", "Medium",
    "Which port is used by DNS for standard name resolution queries?",
    "DNS uses port 53 for both UDP (typical queries) and TCP (zone transfers and larger responses).",
    "Network+ — Objective 1.5",
    [["Port 25", false], ["Port 53", true], ["Port 443", false], ["Port 3389", false]]),
  buildQuestion("304", CERT_NETWORKPLUS, "Infrastructure", "Cabling Standards", "Medium",
    "Which fiber connector type uses a push-pull mechanism and is common in modern datacenter deployments?",
    "The LC connector uses a compact push-pull latch and is widely used in high-density datacenter patch panels, in contrast to the older screw-on SC or ST connectors.",
    "Network+ — Objective 2.1",
    [["ST connector", false], ["SC connector", false], ["LC connector", true], ["BNC connector", false]]),
  buildQuestion("305", CERT_NETWORKPLUS, "Infrastructure", "Wireless", "Hard",
    "Two nearby access points on overlapping 2.4 GHz channels are causing interference. Which channel plan avoids overlap for three co-located APs?",
    "Channels 1, 6, and 11 are the only non-overlapping channels in the 2.4 GHz band in most regulatory domains, making them the standard plan for adjacent APs.",
    "Network+ — Objective 2.4",
    [["Channels 1, 6, 11", true], ["Channels 2, 5, 9", false], ["Channels 1, 2, 3", false], ["Channels 6, 7, 8", false]]),
  buildQuestion("306", CERT_NETWORKPLUS, "Network Operations", "Monitoring", "Medium",
    "Which protocol is commonly used to collect performance and status data from network devices for centralized monitoring?",
    "SNMP (Simple Network Management Protocol) is the standard protocol for polling and receiving status/performance data from managed network devices.",
    "Network+ — Objective 3.1",
    [["SNMP", true], ["FTP", false], ["SMTP", false], ["NTP", false]]),
  buildQuestion("307", CERT_NETWORKPLUS, "Network Operations", "Disaster Recovery", "Medium",
    "What metric describes the maximum acceptable amount of data loss, measured in time, following an outage?",
    "Recovery Point Objective (RPO) defines how much data (in time) an organization can afford to lose, which drives backup frequency decisions.",
    "Network+ — Objective 3.2",
    [["RTO", false], ["RPO", true], ["MTTR", false], ["MTBF", false]]),
  buildQuestion("308", CERT_NETWORKPLUS, "Network Security", "Threats", "Medium",
    "An attacker floods a server with more connection requests than it can handle, making it unavailable to legitimate users. What is this attack called?",
    "This describes a Denial of Service (DoS) attack; when performed from multiple sources simultaneously, it is a Distributed DoS (DDoS).",
    "Network+ — Objective 4.1",
    [["Denial of Service", true], ["ARP spoofing", false], ["DNS poisoning", false], ["VLAN hopping", false]]),
  buildQuestion("309", CERT_NETWORKPLUS, "Network Security", "Hardening", "Easy",
    "Which practice most directly reduces a device's attack surface by removing unused functionality?",
    "Disabling unused ports, services, and protocols directly shrinks the number of possible entry points an attacker can exploit.",
    "Network+ — Objective 4.3",
    [["Disabling unused ports and services", true], ["Increasing Wi-Fi transmit power", false], ["Enabling guest network broadcast", false], ["Installing more applications", false]]),
  buildQuestion("310", CERT_NETWORKPLUS, "Network Troubleshooting", "Methodology", "Medium",
    "After identifying the probable cause of a network issue, what is the next recommended step in standard troubleshooting methodology?",
    "The standard methodology moves from identifying probable cause to establishing a theory, then testing that theory before implementing a full solution.",
    "Network+ — Objective 5.1",
    [["Establish a theory of probable cause and test it", true], ["Immediately replace all hardware", false], ["Close the ticket without further action", false], ["Escalate without gathering any information", false]]),
  buildQuestion("311", CERT_NETWORKPLUS, "Network Troubleshooting", "Tools", "Easy",
    "Which command-line tool traces the path packets take across multiple routers to a destination?",
    "traceroute (or tracert on Windows) reports each hop between source and destination, helping isolate where latency or loss is occurring.",
    "Network+ — Objective 5.2",
    [["traceroute / tracert", true], ["nslookup", false], ["netstat", false], ["ping only", false]]),
  buildQuestion("312", CERT_NETWORKPLUS, "Network Troubleshooting", "IP Configuration", "Easy",
    "A Windows workstation assigns itself an address beginning with 169.254 after startup. What is the most likely cause?",
    "A 169.254.x.x APIPA address is automatically assigned when the workstation cannot obtain a valid lease from a DHCP server.",
    "Network+ — Objective 5.5",
    [["The DHCP server could not be reached", true], ["The DNS server resolved the wrong hostname", false], ["The default gateway is using NAT", false], ["The switch port negotiated full duplex", false]]),
];

export const mockAdminOverview: AdminOverview = {
  totalUsers: 18420,
  premiumUsers: 3120,
  activeUsersToday: 2210,
  usersOnlineNow: 314,
  usersTakingExamsNow: 42,
  practiceSessionsRunning: 128,
  monthlyRevenue: 148500,
  monthlyGrowthPercent: 8.4,
  mostPopularCertification: "CompTIA Network+",
  registrationsByDay: [
    { date: "Mon", count: 120 },
    { date: "Tue", count: 145 },
    { date: "Wed", count: 98 },
    { date: "Thu", count: 165 },
    { date: "Fri", count: 190 },
    { date: "Sat", count: 210 },
    { date: "Sun", count: 175 },
  ],
  systemStatus: { database: "Online", api: "Online", backgroundJobs: "Running" },
};

export const mockMaintenanceWindows: MaintenanceWindow[] = [
  {
    id: "mw-1",
    startsAt: "2026-08-02T02:00:00Z",
    endsAt: "2026-08-02T02:30:00Z",
    reason: "Database index maintenance and question bank reindexing.",
    status: "Scheduled",
  },
];

export const mockExamAttempts: ExamAttempt[] = mockDashboard.recentAttempts;

export const mockNotifications: import("../types").AppNotification[] = [
  {
    id: "notif-1",
    title: "Welcome to CertMaster!",
    body: "Start with a practice session on CompTIA A+ Core 1, Core 2, or Network+ to see where you stand.",
    type: "General",
    isRead: false,
    createdAtUtc: "2026-07-25T08:00:00Z",
  },
  {
    id: "notif-2",
    title: "Mock exam results are in",
    body: "You scored 64% on your last Network+ mock exam (16/25 correct).",
    type: "ExamResult",
    isRead: false,
    createdAtUtc: "2026-07-26T18:25:00Z",
  },
  {
    id: "notif-3",
    title: "Scheduled maintenance",
    body: "CertMaster will be briefly unavailable on Aug 2 from 02:00–02:30 UTC for question bank updates.",
    type: "Maintenance",
    isRead: true,
    createdAtUtc: "2026-07-28T09:10:00Z",
  },
];

export function mockSearch(term: string): import("../types").SearchResult[] {
  const normalized = term.trim().toLowerCase();
  if (normalized.length < 2) return [];

  const results: import("../types").SearchResult[] = [];

  for (const cert of mockCertifications) {
    if (cert.name.toLowerCase().includes(normalized) || cert.code.toLowerCase().includes(normalized)) {
      results.push({ type: "Certification", title: cert.name, subtitle: cert.code, id: cert.id, certificationId: cert.id });
    }
  }

  const seenTopics = new Set<string>();
  for (const q of mockQuestions) {
    if (q.topic.toLowerCase().includes(normalized) && !seenTopics.has(q.topic)) {
      seenTopics.add(q.topic);
      results.push({ type: "Topic", title: q.topic, subtitle: "Practice this topic", id: q.topic, certificationId: q.certificationId });
    }
  }

  for (const q of mockQuestions) {
    if (q.prompt.toLowerCase().includes(normalized)) {
      results.push({ type: "Question", title: q.prompt, subtitle: q.topic, id: q.id, certificationId: q.certificationId });
    }
  }

  return results.slice(0, 8);
}

// Mutable in-memory bookmark state for mock mode — lets the bookmark button and the
// Bookmarks page behave consistently within a session without a real backend.
export const mockBookmarkedIds = new Set<string>(mockQuestions.slice(0, 3).map((q) => q.id));

export const mockLeaderboard: import("../types").LeaderboardEntry[] = [
  { rank: 1, fullName: "Naledi M.", points: 2840, isPremium: true, isCurrentUser: false },
  { rank: 2, fullName: "Sipho K.", points: 2715, isPremium: true, isCurrentUser: false },
  { rank: 3, fullName: "Amara O.", points: 2690, isPremium: false, isCurrentUser: false },
  { rank: 4, fullName: "Liam P.", points: 2540, isPremium: false, isCurrentUser: false },
  { rank: 5, fullName: "Zanele D.", points: 2495, isPremium: true, isCurrentUser: false },
  { rank: 47, fullName: "You", points: 1205, isPremium: false, isCurrentUser: true },
];

export let mockOpenReports: import("../types").QuestionReport[] = [
  {
    id: "report-1",
    questionId: mockQuestions[3]?.id ?? "q-104",
    questionPrompt: mockQuestions[3]?.prompt ?? "Sample flagged question",
    questionStatus: "Flagged",
    reportedByEmail: "sipho@example.com",
    reason: "I think option C might actually be correct too — the 802.11ac standard also supports MU-MIMO.",
    resolved: false,
    createdAtUtc: "2026-07-29T10:15:00Z",
  },
];
