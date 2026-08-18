from __future__ import annotations

from abc import ABC, abstractmethod

from app.models.ocr_result import OcrDocument, ReceiptAnalysis


class ReceiptParser(ABC):
    @abstractmethod
    def matches(self, document: OcrDocument) -> bool:
        raise NotImplementedError

    @abstractmethod
    def parse(self, document: OcrDocument) -> ReceiptAnalysis:
        raise NotImplementedError
