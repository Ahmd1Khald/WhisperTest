from fastapi import FastAPI, UploadFile, File
import whisper
import shutil
import os
from pydub import AudioSegment

app = FastAPI()

# تحميل الموديل مرة واحدة
model = whisper.load_model("large")  # ممكن تستخدم "medium" أو "base" لو جهازك ضعيف

CHUNK_LENGTH_MS = 60_000  # كل دقيقة

def split_audio(file_path, chunk_length_ms=CHUNK_LENGTH_MS):
    audio = AudioSegment.from_file(file_path)
    chunks = []
    for i in range(0, len(audio), chunk_length_ms):
        chunk = audio[i:i + chunk_length_ms]
        chunk_name = f"chunk_{i//chunk_length_ms}.mp3"
        chunk.export(chunk_name, format="mp3")
        chunks.append(chunk_name)
    return chunks

def transcribe_chunks(chunk_files):
    full_text = ""
    for chunk_file in chunk_files:
        result = model.transcribe(chunk_file, language="ar", fp16=False)
        full_text += result["text"].strip() + "\n\n"
        os.remove(chunk_file)
    return full_text.strip()

@app.post("/transcribe/")
async def transcribe(file: UploadFile = File(...)):
    temp_filename = f"temp_{file.filename}"
    
    # حفظ الملف على القرص مؤقتًا
    with open(temp_filename, "wb") as buffer:
        shutil.copyfileobj(file.file, buffer)

    # تقسيم الملف لأجزاء صغيرة
    chunks = split_audio(temp_filename)

    # حذف الملف الأصلي بعد التقسيم
    os.remove(temp_filename)

    # تنفيذ التفريغ لكل جزء
    transcript = transcribe_chunks(chunks)

    return {"text": transcript}
