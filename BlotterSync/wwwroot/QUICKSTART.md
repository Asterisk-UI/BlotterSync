# BlotterSync Standalone - Quick Start Guide

## 🚀 Get Started in 30 Seconds

### Step 1: Open the App
Open this file in your browser:
```
BlotterSync/wwwroot/index-standalone.html
```

### Step 2: Login
Choose one of these options:

**Option A: Demo Account**
- Username: `officer`
- Password: `password123`
- Click **Login**

**Option B: Instant Access**
- Click **"Explore Demo"** button
- Automatically logs in with first officer account

**Option C: Create Account**
- Click **"Create Account"**
- Fill in username and password
- Click **Register**

### Step 3: Start Using
- View incidents on the **Overview** page
- Browse all incidents on **Incidents** page
- File new incidents on **Intake** page
- View history on **History** page
- Manage settings on **Settings** page

**That's it! All data is stored in your browser. No internet required.**

---

## 📝 Common Tasks

### Create a New Incident
1. Click **Intake** in the sidebar
2. Click **"+ New Incident"** button
3. Fill in the form:
   - Category: Select from dropdown
   - Location: Where it happened
   - Description: What happened
   - Date: When it happened
4. Click **"File Incident"**
5. Incident appears in **Incidents** list

### Find an Incident
1. Click **Incidents** in the sidebar
2. Use the **Search** box (top right)
3. Type location or incident number
4. Results filter in real-time

### Filter by Status
1. Click **Incidents** in the sidebar
2. Use the **Status** dropdown (top right)
3. Choose: All, Pending, Ongoing, Resolved, Dismissed
4. List updates immediately

### Change Incident Status
1. Click any incident to view details
2. Click **Edit**
3. Change the **Status** dropdown
4. Click **Save**

### Export Data (Backup)
1. Click **Settings** in the sidebar
2. Click **Export Data**
3. File downloads as JSON
4. Safe to backup or share

### Reset to Demo Data
1. Click **Settings** in the sidebar
2. Click **"Reset to Demo"** button
3. Confirm the dialog
4. Page reloads with original demo data

---

## 🔐 Demo Users

| Username | Password | Notes |
|----------|----------|-------|
| officer | password123 | Regular officer account |
| maria | password123 | Another officer account |
| admin | admin123 | Admin account |

Create your own accounts anytime with the registration form!

---

## 🧪 Test the App

### Run Automated Tests
Open browser console (press `F12`) and run:
```javascript
BlotterSyncTests.runAllTests()
```

Watch the console for test results. Should show:
- ✅ 10 passing tests
- 0 failing tests

### Open Visual Test Dashboard
Open this file in your browser for an interactive test suite:
```
BlotterSync/wwwroot/test-suite.html
```

Click buttons to:
- Test individual features
- View system status
- Export data
- Check storage usage
- Reset demo data

---

## 💾 Understanding Local Storage

All your data is stored in **browser localStorage**:
- **No internet required** to use the app
- **No server** to access the data
- **No cloud sync** between devices
- **Export as JSON** to backup or transfer

### Storage Limits
- Chrome/Firefox/Edge: 10 MB
- Safari: 5 MB
- Current demo data: ~50 KB

### Clear All Data
Open browser console and run:
```javascript
localStorage.clear()
```

Then refresh the page to start fresh with demo data.

---

## 📱 Mobile/Tablet

The app works great on mobile and tablet!

**To save to home screen (PWA-like):**
1. Open the app in browser
2. Tap menu (⋯) or share button
3. Select "Add to Home Screen" or "Add Shortcut"
4. Tap the icon to open the app later

---

## 🔍 Troubleshooting

### App doesn't load
- Check browser console (F12) for errors
- Try refreshing (Ctrl+R or Cmd+R)
- Make sure you're opening `index-standalone.html`

### Data disappeared
- Check localStorage: `localStorage.getItem('blotter_users')`
- Never cleared the app data in browser settings?
- Privacy mode (Incognito) doesn't persist data

### Search/Filter not working
- Try a different search term
- Check that incidents exist in the Incidents page
- Open console and run: `storage.getRecords()` to verify data

### Can't log in
- Check username and password are exact (case-sensitive)
- Try demo account: `officer` / `password123`
- Try creating a new account

### Storage full
- Export current data: Settings → Export Data
- Clear old data: Settings → Reset to Demo
- Delete browser cache for the site

---

## 🎯 Key Features

### ✅ Implemented
- [x] User authentication
- [x] Incident create/read/update/delete
- [x] Search by location or tracking number
- [x] Filter by status
- [x] Intake form for new incidents
- [x] Agent history
- [x] Data export (JSON)
- [x] Responsive design (mobile/tablet/desktop)
- [x] Settings management
- [x] Demo data seeding

### ❌ Not Supported
- [ ] Cloud sync
- [ ] Multi-device access
- [ ] Real OpenAI agent
- [ ] Server authentication
- [ ] Real-time collaboration

---

## 📚 Learn More

For detailed documentation, see:
- **README-STANDALONE.md** - Complete feature documentation
- **IMPLEMENTATION-COMPLETE.md** - Technical implementation details
- **Browser Console** - Run `BlotterSyncTests.runAllTests()`

---

## 🆘 Need Help?

1. **Check the console** (F12) for error messages
2. **Run tests** to verify functionality works
3. **Export your data** (Settings → Export Data)
4. **Reset and try again** (Settings → Reset to Demo)

---

## 🎓 Developer Info

### Project Structure
```
BlotterSync/wwwroot/
├── index-standalone.html      # Login page
├── workspace-standalone.html   # Main app shell
├── workspace-app.js            # Application logic
├── storage.js                  # Data persistence
├── init-data.js               # Demo data
├── workspace-standalone.css    # Styling
├── tests.js                    # Test suite
├── test-suite.html            # Visual tests
└── ui.js                       # UI utilities
```

### How It Works
1. **index-standalone.html** loads storage.js and init-data.js
2. Demo data initializes on first load
3. User logs in (client-side validation)
4. **workspace-standalone.html** loads and runs workspace-app.js
5. All data operations use **storage.js** (localStorage wrapper)
6. No API calls to backend
7. All persistence is browser-local

### Running Tests
```javascript
// In browser console
BlotterSyncTests.runAllTests()

// Or open test-suite.html for visual testing
```

---

## ✨ Tips & Tricks

- **Quick login**: Click "Explore Demo" for instant access
- **Keyboard shortcuts**: 
  - `Escape` to close modals/dialogs
  - `Enter` to submit forms
- **Mobile menu**: Tap hamburger icon to toggle sidebar
- **Dark mode**: Your browser's dark mode setting applies
- **Offline**: Works without any internet connection!

---

**BlotterSync Standalone v1.0** • Pure HTML/CSS/JavaScript • No Backend Required

Created: 2024
Last Updated: Today

Ready to go! 🚀
