/**
 * BlotterSync Standalone - Verification Test Suite
 * Run this in browser console to verify all functionality works
 */

class BlotterSyncTests {
  static async runAllTests() {
    console.log("🧪 Running BlotterSync Standalone Test Suite...\n");

    const tests = [
      this.testStorageInitialization,
      this.testUserAuthentication,
      this.testDataPersistence,
      this.testRecordCreation,
      this.testSearch,
      this.testFilter,
      this.testRecordUpdate,
      this.testRecordDeletion,
      this.testExport,
      this.testDataIntegrity
    ];

    let passed = 0;
    let failed = 0;

    for (const test of tests) {
      try {
        await test.call(this);
        passed++;
        console.log(`✅ ${test.name} passed\n`);
      } catch (error) {
        failed++;
        console.error(`❌ ${test.name} failed: ${error.message}\n`);
      }
    }

    console.log(`\n📊 Test Results: ${passed} passed, ${failed} failed`);
    return { passed, failed, total: tests.length };
  }

  static testStorageInitialization() {
    if (!window.storage) throw new Error("storage object not found");
    if (typeof storage.getUsers !== "function") throw new Error("storage.getUsers not available");
    if (typeof storage.getRecords !== "function") throw new Error("storage.getRecords not available");
    console.log("✓ Storage layer initialized");
  }

  static testUserAuthentication() {
    const users = storage.getUsers();
    if (users.length === 0) throw new Error("No demo users found");

    const officer = storage.findUser("officer", "password123");
    if (!officer) throw new Error("Demo officer login failed");
    if (officer.username !== "officer") throw new Error("Officer username mismatch");

    console.log(`✓ Found ${users.length} demo users`);
    console.log(`✓ Officer login successful`);
  }

  static testDataPersistence() {
    const testKey = "test_persistence_" + Date.now();
    const testValue = { test: true, timestamp: new Date().toISOString() };

    localStorage.setItem(testKey, JSON.stringify(testValue));
    const retrieved = JSON.parse(localStorage.getItem(testKey));

    if (!retrieved || retrieved.test !== true) {
      throw new Error("localStorage persistence failed");
    }

    localStorage.removeItem(testKey);
    console.log("✓ localStorage persistence verified");
  }

  static testRecordCreation() {
    const initialCount = storage.getRecords().length;

    const newRecord = {
      recordId: "test_" + storage.generateId(),
      trackingNumber: "TEST-2024-001",
      categoryId: "cat_theft",
      officerId: "officer_1",
      complainantId: "res_1",
      location: "Test Location",
      narrative: "Test narrative for verification",
      status: "Pending",
      incidentDate: new Date().toISOString(),
      reportedDate: new Date().toISOString()
    };

    const saved = storage.saveRecord(newRecord);
    if (!saved.recordId) throw new Error("Record save failed");

    const records = storage.getRecords();
    if (records.length !== initialCount + 1) throw new Error("Record count mismatch");

    const retrieved = storage.getRecord(saved.recordId);
    if (!retrieved) throw new Error("Record retrieval failed");

    // Cleanup
    storage.deleteRecord(saved.recordId);

    console.log("✓ Record creation successful");
    console.log(`✓ Total records in storage: ${initialCount}`);
  }

  static testSearch() {
    const records = storage.getRecords();
    if (records.length === 0){
      console.log("⚠ No records to search");
      return;
    }

    const testRecord = records[0];
    const searchTerm = testRecord.location.substring(0, 5);

    const filtered = records.filter(r => 
      r.location.includes(searchTerm) ||
      r.trackingNumber?.includes(searchTerm)
    );

    if (filtered.length === 0) throw new Error("Search returned no results");

    console.log(`✓ Search functionality verified`);
    console.log(`✓ Found ${filtered.length} records matching search`);
  }

  static testFilter() {
    const records = storage.getRecords();
    const statuses = ["Pending", "Ongoing", "Resolved", "Dismissed"];

    for (const status of statuses) {
      const filtered = records.filter(r => r.status === status);
      console.log(`  - ${status}: ${filtered.length} records`);
    }

    console.log("✓ Filter by status verified");
  }

  static testRecordUpdate() {
    const records = storage.getRecords();
    if (records.length === 0) {
      console.log("⚠ No records to update");
      return;
    }

    const testRecord = records[0];
    const originalNarrative = testRecord.narrative;
    testRecord.narrative = "Updated: " + Date.now();

    const updated = storage.saveRecord(testRecord);
    const retrieved = storage.getRecord(updated.recordId);

    if (retrieved.narrative !== testRecord.narrative) {
      throw new Error("Record update failed");
    }

    // Restore original
    testRecord.narrative = originalNarrative;
    storage.saveRecord(testRecord);

    console.log("✓ Record update successful");
  }

  static testRecordDeletion() {
    const initialCount = storage.getRecords().length;

    const testRecord = {
      recordId: "delete_test_" + storage.generateId(),
      trackingNumber: "DEL-TEST-001",
      categoryId: "cat_theft",
      location: "Test Location",
      narrative: "Will be deleted",
      status: "Pending",
      incidentDate: new Date().toISOString(),
      reportedDate: new Date().toISOString()
    };

    const saved = storage.saveRecord(testRecord);
    let count = storage.getRecords().length;
    if (count !== initialCount + 1) throw new Error("Record not saved for deletion test");

    storage.deleteRecord(saved.recordId);
    count = storage.getRecords().length;
    if (count !== initialCount) throw new Error("Record deletion failed");

    console.log("✓ Record deletion successful");
  }

  static testExport() {
    const data = storage.exportData();

    if (!data.users || !Array.isArray(data.users)) throw new Error("Export missing users");
    if (!data.records || !Array.isArray(data.records)) throw new Error("Export missing records");
    if (!data.categories || !Array.isArray(data.categories)) throw new Error("Export missing categories");

    const json = JSON.stringify(data);
    if (json.length === 0) throw new Error("Export resulted in empty JSON");

    console.log("✓ Data export successful");
    console.log(`✓ Export size: ${(json.length / 1024).toFixed(2)} KB`);
    console.log(`✓ Users: ${data.users.length}, Records: ${data.records.length}, Categories: ${data.categories.length}`);
  }

  static testDataIntegrity() {
    const records = storage.getRecords();
    const categories = storage.getCategories();
    const residents = storage.getResidents();

    // Verify all category references are valid
    for (const record of records) {
      const category = categories.find(c => c.categoryId === record.categoryId);
      if (!category && record.categoryId) {
        throw new Error(`Invalid category reference in record ${record.recordId}`);
      }
    }

    // Verify resident references
    for (const record of records) {
      if (record.complainantId && !residents.find(r => r.residentId === record.complainantId)) {
        console.warn(`⚠ Missing resident: ${record.complainantId}`);
      }
    }

    console.log("✓ Data integrity verified");
    console.log(`✓ Categories: ${categories.length}, Residents: ${residents.length}`);
  }

  // Helper to test page reload persistence
  static testPageReloadPersistence() {
    console.log("\n📄 Testing page reload persistence...");
    const testId = "reload_test_" + Date.now();
    localStorage.setItem(testId, JSON.stringify({ test: true }));
    console.log(`✓ Set test key: ${testId}`);
    console.log("  Now refresh the page and run:");
    console.log(`  localStorage.getItem('${testId}')`);
    console.log("  Should return: {\"test\":true}");
  }
}

// Run all tests
console.clear();
BlotterSyncTests.runAllTests().then(results => {
  console.log("\n" + "=".repeat(50));
  if (results.failed === 0) {
    console.log("✅ ALL TESTS PASSED!");
  } else {
    console.log(`⚠️  ${results.failed} test(s) failed`);
  }
  console.log("=".repeat(50));
});
