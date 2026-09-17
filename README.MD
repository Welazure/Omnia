Deskripsi Masalah
Proses memindahkan teks pendek, tautan (URL), atau catatan kecil antar perangkat (misalnya dari ponsel ke laptop) seringkali lambat dan tidak efisien. Pengguna terpaksa mengandalkan aplikasi pesan instan dengan mengirim pesan ke diri sendiri atau menggunakan email, yang menyebabkan alur kerja terputus dan riwayat obrolan menjadi berantakan.

Profil Target Pengguna

    Developer & Desainer: Mereka yang sering beralih antara lingkungan pengembangan di PC dan pengujian di ponsel genggam.

    Mahasiswa & Pekerja Digital: Individu yang membutuhkan ruang kerja terpadu saat melakukan riset, menulis, atau menyusun laporan menggunakan lebih dari satu perangkat secara bersamaan.

Manfaat Aplikasi

    Efisiensi Waktu: Memangkas langkah-langkah sinkronisasi data ringan menjadi instan dan tanpa gesekan.

    Fokus Terjaga: Memisahkan catatan sementara (scratchpad) dari ruang komunikasi pribadi, menjaga ruang kerja tetap rapi.

    Aksesibilitas Universal: Bekerja mulus menembus batas ekosistem sistem operasi karena dibangun sebagai solusi lintas platform.

Daftar Fitur Inti (Realistis untuk 12 Pertemuan)

    Single-Screen Interface: Antarmuka minimalis tanpa navigasi rumit; aplikasi terbuka langsung ke kolom input teks yang aktif.

    Real-time Cloud Sync: Sinkronisasi instan teks dan daftar tugas antar perangkat menggunakan Firebase atau Supabase.

    Anonymous Room Code: Sistem penghubung perangkat berbasis PIN 4-digit agar perangkat dapat berbagi clipboard tanpa perlu registrasi atau login akun email.

    One-Tap Copy & Swipe Actions: Ketuk item untuk menyalin teks ke clipboard sistem, dan geser (swipe) untuk menghapusnya dari basis data cloud.

Fitur yang Tidak Dikerjakan (Out of Scope)

    Berbagi file media (gambar, video, atau dokumen PDF).

    Sistem autentikasi pengguna yang kompleks (seperti OAuth, Google/Apple Sign-In, atau pemulihan kata sandi).

    Penyimpanan data permanen atau pengarsipan folder jangka panjang (aplikasi difokuskan pada data sementara).

    Implementasi Enkripsi End-to-End (E2EE) tingkat lanjut.

Kriteria Aplikasi Dinyatakan Berhasil

    Aplikasi berhasil di-build dan berjalan stabil menggunakan Flutter di minimal dua platform berbeda (misalnya: Android dan Web/Windows).

    Latensi sinkronisasi: Teks yang diinput pada perangkat A muncul di perangkat B yang berada dalam Room Code yang sama dalam waktu kurang dari 2 detik.

    Fitur salin otomatis ke clipboard bawaan sistem operasi berfungsi dengan akurat pada semua perangkat yang diuji.

    Seluruh alur kerja (buat ruangan, input data, sinkronisasi, salin, dan hapus) dapat dijalankan tanpa crash.