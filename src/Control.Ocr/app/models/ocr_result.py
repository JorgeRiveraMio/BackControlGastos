from __future__ import annotations

from typing import Literal

from pydantic import BaseModel, Field


class BoundingBox(BaseModel):
    left: float
    top: float
    right: float
    bottom: float

    @property
    def width(self) -> float:
        return self.right - self.left

    @property
    def height(self) -> float:
        return self.bottom - self.top

    @property
    def center_y(self) -> float:
        return (self.top + self.bottom) / 2


class OcrLine(BaseModel):
    text: str
    confidence: float | None = Field(default=None, ge=0, le=1)
    box: BoundingBox | None = None
    polygon: list[tuple[float, float]] = Field(default_factory=list)
    reading_order: int


class OcrDocument(BaseModel):
    lines: list[OcrLine]
    image_width: int | None = None
    image_height: int | None = None


class ReceiptAnalysis(BaseModel):
    document_type: Literal["YAPE", "PLIN", "CARD_VOUCHER", "RECEIPT", "UNKNOWN"]
    amount: float | None = None
    amount_raw: str | None = None
    currency: str | None = None
    date: str | None = None
    recipient: str | None = None
    recipient_phone: str | None = None
    destination: str | None = None
    operation_number: str | None = None
    confidence: float = Field(ge=0, le=1)
    requires_review: bool
    ocr_lines: list[OcrLine]
