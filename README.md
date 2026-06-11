# Tekla Sequence Selector

Aplikasi Windows Form untuk men-select steel objects di Tekla Structures berdasarkan sequence/phase number.

## ✨ Fitur

✅ **Button Sequence (SEQ-1 hingga SEQ-10)**
- Click tombol untuk menambah/menghapus sequence dari seleksi
- Tombol yang terpilih akan berubah warna menjadi biru

✅ **Custom Sequence Input**
- Input sequence dengan format: `SEQ-15`, `SEQ-20`, atau hanya `15`
- Support multiple input dengan separator `,` atau `;`
- Contoh: `15, 20, 25` atau `SEQ-15; SEQ-20`

✅ **Single Select & Multi-Sequence Support**
- Pilih multiple sequence sekaligus
- Lihat daftar semua sequence yang dipilih di listbox

✅ **Tekla Integration**
- Connect otomatis ke Tekla Structures
- Select all steel objects berdasarkan phase/sequence yang dipilih
- Siap untuk di-generate report

## 🎮 Cara Penggunaan

1. **Buka aplikasi** - Pastikan Tekla Structures sudah running
2. **Pilih Sequence**:
   - Click button SEQ-1 sampai SEQ-10, atau
   - Input custom sequence di text box (misal: `15, 20`) lalu click **Add**
3. **Review Selections** - Lihat daftar di listbox "Selected Sequences"
4. **Select Objects** - Click tombol **"Select Objects in Tekla"**
5. **Export/Report** - Gunakan data terpilih untuk membuat report

## 🔘 Tombol Fungsi

| Tombol | Fungsi |
|--------|--------|
| **SEQ-1 s/d SEQ-10** | Toggle sequence ke dalam/dari seleksi |
| **Add** | Tambah custom sequence dari input text |
| **Select Objects in Tekla** | Select semua steel objects sesuai sequence |
| **Clear All** | Hapus semua sequence dari seleksi |
| **Remove Selected** | Hapus sequence yang dipilih di listbox |

## 📋 Requirements

- Visual Studio 2019 atau lebih baru
- Tekla Structures 2019 atau lebih baru
- .NET Framework 4.7.2+
- Tekla Open API

## 🔧 Setup

1. Clone repository:
   ```bash
   git clone https://github.com/alesmana4u/tekla-selector.git
   ```

2. Buka di Visual Studio

3. Add reference ke Tekla API:
   - Buka `TeklaSelector.csproj`
   - Uncomment reference lines untuk Tekla DLL
   - Sesuaikan path dengan instalasi Tekla Anda (misal: `C:\Program Files\Tekla Structures\2021\bin\`)

4. Build & Run
   ```bash
   Ctrl + F5 (Run without debugging)
   ```

## 📁 Struktur File

```
tekla-selector/
├── Program.cs              # Entry point aplikasi
├── Main1.cs               # Logic utama & event handlers
├── Main1.Designer.cs      # UI components (auto-generated)
├── Main1.resx            # Resources
├── App.config            # Configuration
├── TeklaSelector.csproj  # Project file
└── README.md             # Documentation
```

## 💡 Cara Kerja

### 1. Selection Logic
- Saat tombol SEQ clicked → sequence ditambahkan ke `HashSet`
- Custom input di-parse dan ditambahkan ke collection
- ListBox di-update dengan daftar semua sequence yang dipilih (sorted by number)

### 2. Tekla Integration
- Aplikasi terhubung ke Tekla Model saat startup
- Saat button "Select Objects" di-click → iterate semua objects di Tekla
- Cek property "PHASE" dari setiap object
- Jika phase match dengan selected sequences → select object tersebut

### 3. Phase Detection
Aplikasi mencari phase dari:
1. User-defined property: `PHASE`
2. Report property: `PHASE`
3. Custom property sesuai kebutuhan project

## 🔍 Debugging Tips

**Error: "Tekla Structures is not running"**
- Pastikan Tekla Structures sudah dibuka
- Buka sebuah project di Tekla terlebih dahulu

**Objects tidak ter-select**
- Cek apakah objects memiliki property "PHASE"
- Lihat console output untuk error message
- Modify `GetPhaseFromObject()` method sesuai property Anda

**Custom Sequence tidak recognize**
- Format input: `15,20,25` atau `SEQ-15; SEQ-20`
- Jangan gunakan space sebelum separator

## 📝 Notes

- Aplikasi akan mencari phase/sequence dari property object di Tekla
- Pastikan steel objects sudah memiliki phase information
- Support custom phase format sesuai project requirement
- Single user application (single connection to Tekla)

## 🚀 Future Enhancements

- [ ] Export selected objects ke CSV/Excel
- [ ] Save/Load selection presets
- [ ] Batch operation pada selected objects
- [ ] Phase validation checker
- [ ] Multi-user support dengan database

## 📧 Support

Untuk pertanyaan atau issues, silakan create GitHub Issue di repository ini.
