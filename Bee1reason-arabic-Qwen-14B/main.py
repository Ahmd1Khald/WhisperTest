from fastapi import FastAPI
from pydantic import BaseModel
from transformers import AutoTokenizer, AutoModelForCausalLM, pipeline
import torch

app = FastAPI()

# ✅ تحميل موديل دردشة يدعم الفهم والتعليمات
model_name = "Qwen/Qwen1.5-1.8B-Chat"

tokenizer = AutoTokenizer.from_pretrained(model_name, trust_remote_code=True)
model = AutoModelForCausalLM.from_pretrained(model_name, trust_remote_code=True)

device = torch.device("cpu")
model.to(device)

# ✅ إعداد واجهة التلخيص الذكي
pipe = pipeline("text-generation", model=model, tokenizer=tokenizer, device=-1)

class MeetingRequest(BaseModel):
    transcript: str

@app.post("/analyze_meeting/")
async def analyze_meeting(request: MeetingRequest):
    prompt = f"""
لديك نص مكتوب لاجتماع فريق عمل.

أريد منك:
1. تلخيص النقاط الرئيسية التي تم مناقشتها.
2. استخراج جميع المهام التي تم تحديدها مع ذكر الشخص المسؤول (إن وُجد).
3. استخراج كل التواريخ أو الـ Deadlines المذكورة.
4. تنظيم الإجابة بشكل واضح ومنظم.

نص الاجتماع:
\"\"\"
{request.transcript}
\"\"\"
    """

    result = pipe(prompt, max_new_tokens=512, do_sample=False)[0]["generated_text"]

    return {"analysis": result}
