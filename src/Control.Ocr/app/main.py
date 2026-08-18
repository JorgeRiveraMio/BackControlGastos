import os
import tempfile
from pathlib import Path

from fastapi import FastAPI, File, HTTPException, UploadFile
from starlette.concurrency import run_in_threadpool

from app.parsers import DocumentClassifier, DocumentType, GenericReceiptParser, PlinParser, YapeParser
from app.models.ocr_result import OcrDocument, ReceiptAnalysis
from app.services.image_preprocessor import ImagePreprocessor
from app.services.ocr_service import OcrService


app = FastAPI(
    title="Control Gastos OCR API",
    version="1.0.0"
)

ocr_service = OcrService()
image_preprocessor = ImagePreprocessor()
classifier = DocumentClassifier()
parsers = {
    DocumentType.YAPE: YapeParser(),
    DocumentType.PLIN: PlinParser(classifier),
    DocumentType.GENERIC: GenericReceiptParser(),
}


@app.get("/health")
async def health():
    return {
        "status": "ok"
    }


@app.post("/api/ocr/receipt", response_model=ReceiptAnalysis)
async def analyze_receipt(file: UploadFile = File(...)) -> ReceiptAnalysis:
    content = await file.read()
    if not content:
        raise HTTPException(status_code=400, detail="The uploaded file is empty.")

    suffix = os.path.splitext(file.filename or "")[1]
    if suffix.lower() not in {".jpg", ".jpeg", ".png", ".webp"}:
        raise HTTPException(status_code=415, detail="Upload a JPG, PNG, or WebP image.")

    with tempfile.NamedTemporaryFile(
        delete=False,
        suffix=suffix
    ) as temp_file:
        temp_file.write(content)
        temp_path = temp_file.name

    prepared_path = f"{temp_path}.prepared{image_preprocessor.prepared_suffix(temp_path)}"

    try:
        await run_in_threadpool(image_preprocessor.prepare, temp_path, prepared_path)
        document = await run_in_threadpool(ocr_service.read_document, prepared_path)
        return parse_receipt(document)
    except ValueError as error:
        raise HTTPException(status_code=400, detail=str(error)) from error
    finally:
        for path in (temp_path, prepared_path):
            Path(path).unlink(missing_ok=True)


def parse_receipt(document: OcrDocument) -> ReceiptAnalysis:
    parser = parsers[classifier.detect(document)]
    return parser.parse(document)
