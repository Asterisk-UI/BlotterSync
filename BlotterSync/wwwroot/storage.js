/**
 * Storage Abstraction Layer
 * Provides localStorage and optional IndexedDB persistence for BlotterSync standalone app
 */
class BlotterStorage {
  constructor() {
    this.storeName = 'blotter_sync_db';
    this.dbName = 'BlotterSyncDB';
    this.db = null;
    this.useIndexedDB = !this.isPrivateMode();
  }

  /**
   * Check if browser is in private/incognito mode
   */
  isPrivateMode() {
    try {
      const test = '__test__';
      localStorage.setItem(test, test);
      localStorage.removeItem(test);
      return false;
    } catch {
      return true;
    }
  }

  /**
   * Initialize IndexedDB (optional)
   */
  async init() {
    if (!this.useIndexedDB) return;

    return new Promise((resolve, reject) => {
      const request = indexedDB.open(this.dbName, 1);

      request.onerror = () => {
        console.log('IndexedDB not available, using localStorage only');
        this.useIndexedDB = false;
        resolve();
      };

      request.onsuccess = () => {
        this.db = request.result;
        resolve();
      };

      request.onupgradeneeded = (e) => {
        const db = e.target.result;
        if (!db.objectStoreNames.contains(this.storeName)) {
          db.createObjectStore(this.storeName, { keyPath: 'id', autoIncrement: true });
        }
      };
    });
  }

  /**
   * Get all users (for authentication)
   */
  getUsers() {
    const users = localStorage.getItem('blotter_users');
    return users ? JSON.parse(users) : [];
  }

  /**
   * Save or update a user
   */
  saveUser(user) {
    const users = this.getUsers();
    const existing = users.findIndex(u => u.officerId === user.officerId);
    if (existing >= 0) {
      users[existing] = { ...users[existing], ...user };
    } else {
      users.push(user);
    }
    localStorage.setItem('blotter_users', JSON.stringify(users));
  }

  /**
   * Find user by username and password
   */
  findUser(username, password) {
    const users = this.getUsers();
    return users.find(u => u.username === username && u.password === password);
  }

  /**
   * Get current session user
   */
  getCurrentUser() {
    const userId = localStorage.getItem('blotter_current_user');
    if (!userId) return null;
    const users = this.getUsers();
    return users.find(u => u.officerId === userId);
  }

  /**
   * Set current session
   */
  setCurrentUser(user) {
    localStorage.setItem('blotter_current_user', user.officerId);
    localStorage.setItem('jwtToken', `demo_token_${user.officerId}`); // Fake JWT for compatibility
  }

  /**
   * Logout current user
   */
  logout() {
    localStorage.removeItem('blotter_current_user');
    localStorage.removeItem('jwtToken');
  }

  /**
   * Get all blotter records
   */
  getRecords() {
    const records = localStorage.getItem('blotter_records');
    return records ? JSON.parse(records) : [];
  }

  /**
   * Save or update a blotter record
   */
  saveRecord(record) {
    const records = this.getRecords();
    const existing = records.findIndex(r => r.recordId === record.recordId);
    if (existing >= 0) {
      records[existing] = { ...records[existing], ...record };
    } else {
      record.recordId = record.recordId || this.generateId();
      records.push(record);
    }
    localStorage.setItem('blotter_records', JSON.stringify(records));
    return record;
  }

  /**
   * Get a single record
   */
  getRecord(recordId) {
    const records = this.getRecords();
    return records.find(r => r.recordId === recordId);
  }

  /**
   * Delete a record
   */
  deleteRecord(recordId) {
    const records = this.getRecords();
    const filtered = records.filter(r => r.recordId !== recordId);
    localStorage.setItem('blotter_records', JSON.stringify(filtered));
  }

  /**
   * Get all categories
   */
  getCategories() {
    const categories = localStorage.getItem('blotter_categories');
    return categories ? JSON.parse(categories) : [];
  }

  /**
   * Save categories
   */
  saveCategories(categories) {
    localStorage.setItem('blotter_categories', JSON.stringify(categories));
  }

  /**
   * Get all residents
   */
  getResidents() {
    const residents = localStorage.getItem('blotter_residents');
    return residents ? JSON.parse(residents) : [];
  }

  /**
   * Save or update a resident
   */
  saveResident(resident) {
    const residents = this.getResidents();
    const existing = residents.findIndex(r => r.residentId === resident.residentId);
    if (existing >= 0) {
      residents[existing] = { ...residents[existing], ...resident };
    } else {
      resident.residentId = resident.residentId || this.generateId();
      residents.push(resident);
    }
    localStorage.setItem('blotter_residents', JSON.stringify(residents));
    return resident;
  }

  /**
   * Search residents by name
   */
  searchResidents(query) {
    const residents = this.getResidents();
    const lowerQuery = query.toLowerCase();
    return residents.filter(r =>
      r.firstName?.toLowerCase().includes(lowerQuery) ||
      r.lastName?.toLowerCase().includes(lowerQuery) ||
      `${r.firstName} ${r.lastName}`.toLowerCase().includes(lowerQuery)
    );
  }

  /**
   * Get all agent runs (AI assistant history)
   */
  getRuns() {
    const runs = localStorage.getItem('blotter_runs');
    return runs ? JSON.parse(runs) : [];
  }

  /**
   * Save or update an agent run
   */
  saveRun(run) {
    const runs = this.getRuns();
    const existing = runs.findIndex(r => r.runId === run.runId);
    if (existing >= 0) {
      runs[existing] = { ...runs[existing], ...run };
    } else {
      run.runId = run.runId || this.generateId();
      runs.push(run);
    }
    localStorage.setItem('blotter_runs', JSON.stringify(runs));
    return run;
  }

  /**
   * Get sync status
   */
  getSyncStatus() {
    const status = localStorage.getItem('blotter_sync_status');
    return status ? JSON.parse(status) : {
      enabled: false,
      cloudOnline: false,
      pendingChanges: 0
    };
  }

  /**
   * Update sync status
   */
  setSyncStatus(status) {
    localStorage.setItem('blotter_sync_status', JSON.stringify(status));
  }

  /**
   * Get announcements
   */
  getAnnouncements() {
    const announcements = localStorage.getItem('blotter_announcements');
    return announcements ? JSON.parse(announcements) : [];
  }

  /**
   * Get active announcement
   */
  getActiveAnnouncement() {
    const announcements = this.getAnnouncements();
    return announcements.find(a => a.active) || null;
  }

  /**
   * Save announcement
   */
  saveAnnouncement(announcement) {
    const announcements = this.getAnnouncements();
    announcements.push(announcement);
    localStorage.setItem('blotter_announcements', JSON.stringify(announcements));
  }

  /**
   * Clear all demo data
   */
  clearAll() {
    const keys = [
      'blotter_users',
      'blotter_records',
      'blotter_categories',
      'blotter_residents',
      'blotter_runs',
      'blotter_sync_status',
      'blotter_announcements',
      'blotter_current_user',
      'jwtToken'
    ];
    keys.forEach(key => localStorage.removeItem(key));
  }

  /**
   * Generate unique ID
   */
  generateId() {
    return Date.now().toString(36) + Math.random().toString(36).substr(2);
  }

  /**
   * Export all data as JSON
   */
  exportData() {
    return {
      users: this.getUsers(),
      records: this.getRecords(),
      categories: this.getCategories(),
      residents: this.getResidents(),
      runs: this.getRuns(),
      syncStatus: this.getSyncStatus(),
      announcements: this.getAnnouncements(),
      exportDate: new Date().toISOString()
    };
  }

  /**
   * Import data from JSON
   */
  importData(data) {
    if (data.users) localStorage.setItem('blotter_users', JSON.stringify(data.users));
    if (data.records) localStorage.setItem('blotter_records', JSON.stringify(data.records));
    if (data.categories) localStorage.setItem('blotter_categories', JSON.stringify(data.categories));
    if (data.residents) localStorage.setItem('blotter_residents', JSON.stringify(data.residents));
    if (data.runs) localStorage.setItem('blotter_runs', JSON.stringify(data.runs));
    if (data.syncStatus) localStorage.setItem('blotter_sync_status', JSON.stringify(data.syncStatus));
    if (data.announcements) localStorage.setItem('blotter_announcements', JSON.stringify(data.announcements));
  }
}

// Global storage instance
const storage = new BlotterStorage();
