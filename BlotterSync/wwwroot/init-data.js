/**
 * Initialization Data for BlotterSync Standalone App
 * Seeds demo users, categories, incidents, and residents on first run
 */

const INIT_DATA = {
  // Demo users for testing
  users: [
    {
      officerId: 'officer_1',
      badgeNumber: 'PO1-001',
      firstName: 'Juan',
      lastName: 'Santos',
      username: 'officer',
      password: 'password123',
      role: 'Officer',
      active: true,
      demo: true,
      email: 'juan.santos@barangay.gov.ph'
    },
    {
      officerId: 'officer_2',
      badgeNumber: 'PO1-002',
      firstName: 'Maria',
      lastName: 'Garcia',
      username: 'maria',
      password: 'password123',
      role: 'Officer',
      active: true,
      demo: true,
      email: 'maria.garcia@barangay.gov.ph'
    },
    {
      officerId: 'admin_1',
      badgeNumber: 'PO1-999',
      firstName: 'Administrator',
      lastName: 'Account',
      username: 'admin',
      password: 'admin123',
      role: 'Admin',
      active: true,
      demo: true,
      email: 'admin@barangay.gov.ph'
    }
  ],

  // Incident categories with severity levels
  categories: [
    { categoryId: 'cat_theft', name: 'Theft / Robbery', severity: 3, active: true },
    { categoryId: 'cat_assault', name: 'Physical Injury / Assault', severity: 4, active: true },
    { categoryId: 'cat_vandalism', name: 'Vandalism / Property Damage', severity: 2, active: true },
    { categoryId: 'cat_noise', name: 'Disturbance of Peace / Noise', severity: 1, active: true },
    { categoryId: 'cat_dispute', name: 'Neighbor Dispute / Quarrel', severity: 1, active: true },
    { categoryId: 'cat_trespassing', name: 'Trespassing', severity: 2, active: true },
    { categoryId: 'cat_intoxication', name: 'Public Intoxication / Disorderly Conduct', severity: 1, active: true },
    { categoryId: 'cat_traffic', name: 'Traffic Violation / Parking Issue', severity: 1, active: true },
    { categoryId: 'cat_weapons', name: 'Illegal Weapons / Firearms', severity: 5, active: true },
    { categoryId: 'cat_drugs', name: 'Drug-Related Incident', severity: 4, active: true },
    { categoryId: 'cat_lost', name: 'Lost / Missing Person', severity: 3, active: true },
    { categoryId: 'cat_fire', name: 'Fire / Emergency', severity: 5, active: true }
  ],

  // Sample residents for testing
  residents: [
    {
      residentId: 'res_1',
      firstName: 'Pedro',
      lastName: 'Reyes',
      contactNumber: '09171234567',
      address: '123 Main St, Barangay Malanday',
      zone: '1',
      active: true
    },
    {
      residentId: 'res_2',
      firstName: 'Angela',
      lastName: 'Cruz',
      contactNumber: '09187654321',
      address: '456 Oak Ave, Barangay Malanday',
      zone: '2',
      active: true
    },
    {
      residentId: 'res_3',
      firstName: 'Roberto',
      lastName: 'Silva',
      contactNumber: '09165432109',
      address: '789 Pine Rd, Barangay Malanday',
      zone: '1',
      active: true
    },
    {
      residentId: 'res_4',
      firstName: 'Lourdes',
      lastName: 'Fernandez',
      contactNumber: '09193456789',
      address: '321 Elm St, Barangay Malanday',
      zone: '3',
      active: true
    },
    {
      residentId: 'res_5',
      firstName: 'Carlos',
      lastName: 'Manalo',
      contactNumber: '09129876543',
      address: '654 Maple Dr, Barangay Malanday',
      zone: '2',
      active: true
    }
  ],

  // Sample blotter records
  records: [
    {
      recordId: 'rec_1',
      trackingNumber: 'BL-2024-001',
      categoryId: 'cat_theft',
      officerId: 'officer_1',
      complainantId: 'res_1',
      respondentId: 'res_3',
      incidentDate: new Date(Date.now() - 7 * 24 * 60 * 60 * 1000).toISOString(),
      reportedDate: new Date(Date.now() - 6 * 24 * 60 * 60 * 1000).toISOString(),
      location: 'Main Street near market',
      narrative: 'Theft of motorcycle parts reported by complainant. Suspect allegedly took turn signals and side mirrors.',
      status: 'Resolved',
      resolutionDate: new Date(Date.now() - 2 * 24 * 60 * 60 * 1000).toISOString()
    },
    {
      recordId: 'rec_2',
      trackingNumber: 'BL-2024-002',
      categoryId: 'cat_dispute',
      officerId: 'officer_1',
      complainantId: 'res_2',
      respondentId: 'res_4',
      incidentDate: new Date(Date.now() - 5 * 24 * 60 * 60 * 1000).toISOString(),
      reportedDate: new Date(Date.now() - 4 * 24 * 60 * 60 * 1000).toISOString(),
      location: 'Barangay Hall vicinity',
      narrative: 'Neighbor dispute over property boundary. Parties involved in verbal altercation regarding fence placement.',
      status: 'Ongoing'
    },
    {
      recordId: 'rec_3',
      trackingNumber: 'BL-2024-003',
      categoryId: 'cat_noise',
      officerId: 'officer_2',
      complainantId: 'res_3',
      respondentId: 'res_5',
      incidentDate: new Date(Date.now() - 3 * 24 * 60 * 60 * 1000).toISOString(),
      reportedDate: new Date(Date.now() - 3 * 24 * 60 * 60 * 1000).toISOString(),
      location: 'Residential area - Zone 2',
      narrative: 'Noise complaint during late evening. Music from residential unit disturbing neighboring residents after 10 PM.',
      status: 'Pending'
    },
    {
      recordId: 'rec_4',
      trackingNumber: 'BL-2024-004',
      categoryId: 'cat_vandalism',
      officerId: 'officer_2',
      complainantId: 'res_4',
      respondentId: null,
      incidentDate: new Date(Date.now() - 2 * 24 * 60 * 60 * 1000).toISOString(),
      reportedDate: new Date(Date.now() - 2 * 24 * 60 * 60 * 1000).toISOString(),
      location: 'Public park - Malanday Avenue',
      narrative: 'Graffiti found on playground equipment and park benches. Suspected youth involvement. Park commissioner notified.',
      status: 'Ongoing'
    },
    {
      recordId: 'rec_5',
      trackingNumber: 'BL-2024-005',
      categoryId: 'cat_trespassing',
      officerId: 'officer_1',
      complainantId: 'res_5',
      respondentId: 'res_2',
      incidentDate: new Date(Date.now() - 1 * 24 * 60 * 60 * 1000).toISOString(),
      reportedDate: new Date(Date.now() - 1 * 24 * 60 * 60 * 1000).toISOString(),
      location: 'Private residential property - Zone 1',
      narrative: 'Unauthorized entry into private property. Owner reported finding unauthorized person in backyard. Suspect fled upon discovery.',
      status: 'Pending'
    },
    {
      recordId: 'rec_6',
      trackingNumber: 'BL-2024-006',
      categoryId: 'cat_lost',
      officerId: 'officer_2',
      complainantId: 'res_1',
      respondentId: null,
      incidentDate: new Date(Date.now() - 4 * 24 * 60 * 60 * 1000).toISOString(),
      reportedDate: new Date(Date.now() - 4 * 24 * 60 * 60 * 1000).toISOString(),
      location: 'Barangay Malanday - General area',
      narrative: 'Missing elderly resident, 74 years old. Last seen near community center. Family requesting assistance. Photos attached to file.',
      status: 'Resolved',
      resolutionDate: new Date(Date.now() - 1 * 24 * 60 * 60 * 1000).toISOString()
    }
  ],

  // Sample agent runs (AI assistant history)
  runs: [
    {
      runId: 'run_1',
      officerId: 'officer_1',
      recordId: 'rec_1',
      status: 'accepted',
      mode: 'local',
      sourceText: 'Motorcycle parts stolen from Main Street',
      generatedDraft: 'Theft of motorcycle components reported. Complainant states that turn signals and side mirrors were taken from motorcycle parked near market.',
      createdAt: new Date(Date.now() - 5 * 24 * 60 * 60 * 1000).toISOString(),
      reviewedAt: new Date(Date.now() - 4 * 24 * 60 * 60 * 1000).toISOString(),
      reviewNotes: 'Looks good. Added suspect information.'
    },
    {
      runId: 'run_2',
      officerId: 'officer_1',
      recordId: null,
      status: 'needs_review',
      mode: 'local',
      sourceText: 'Gate left open, suspicious activity near property',
      generatedDraft: 'Trespassing incident reported. Unauthorized person observed near private property. Gate found open, indicating possible forced entry or negligent closure.',
      createdAt: new Date(Date.now() - 2 * 24 * 60 * 60 * 1000).toISOString(),
      reviewedAt: null,
      reviewNotes: null
    }
  ],

  // Sync status
  syncStatus: {
    enabled: false,
    cloudOnline: false,
    pendingChanges: 0,
    lastSyncTime: null
  }
};

/**
 * Initialize demo data in localStorage if not already done
 */
function initializeDemoData() {
  const initialized = localStorage.getItem('blotter_initialized');

  if (!initialized) {
    console.log('Initializing BlotterSync demo data...');

    // Save all demo data to localStorage
    localStorage.setItem('blotter_users', JSON.stringify(INIT_DATA.users));
    localStorage.setItem('blotter_categories', JSON.stringify(INIT_DATA.categories));
    localStorage.setItem('blotter_residents', JSON.stringify(INIT_DATA.residents));
    localStorage.setItem('blotter_records', JSON.stringify(INIT_DATA.records));
    localStorage.setItem('blotter_runs', JSON.stringify(INIT_DATA.runs));
    localStorage.setItem('blotter_sync_status', JSON.stringify(INIT_DATA.syncStatus));
    localStorage.setItem('blotter_initialized', 'true');

    console.log('✓ Demo data initialized successfully');
    console.log('Test users:');
    console.log('  • Officer: officer / password123');
    console.log('  • Admin: admin / admin123');
  }
}

/**
 * Reset demo data to initial state
 */
function resetDemoData() {
  console.log('Resetting demo data to initial state...');
  localStorage.removeItem('blotter_initialized');
  localStorage.removeItem('blotter_current_user');
  localStorage.removeItem('jwtToken');
  initializeDemoData();
  console.log('✓ Demo data reset');
}

// Auto-initialize on load if needed
if (typeof document !== 'undefined') {
  document.addEventListener('DOMContentLoaded', initializeDemoData);
} else {
  initializeDemoData();
}
