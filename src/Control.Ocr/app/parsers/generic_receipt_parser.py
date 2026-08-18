from __future__ import annotations

import re

from app.models.ocr_result import OcrDocument, OcrLine, ReceiptAnalysis
from app.parsers.receipt_parser import ReceiptParser

MONEY_PATTERN = re.compile(r"(?:S/|PEN|\$)\s*(\d+(?:[.,]\d{2})?)", re.IGNORECASE)
TOTAL_LABEL_PATTERN = re.compile(r"\b(total|importe|monto)\b", re.IGNORECASE)


class GenericReceiptParser(ReceiptParser):
    def matches(self, document: OcrDocument) -> bool:
        return True

    def parse(self, document: OcrDocument) -> ReceiptAnalysis:
        candidate = self._best_explicit_amount(document.lines)
        if candidate is None:
            return ReceiptAnalysis(
                document_type="RECEIPT",
                confidence=0.0,
                requires_review=True,
                ocr_lines=document.lines,
            )

        line, value, currency = candidate
        return ReceiptAnalysis(
            document_type="RECEIPT",
            amount=value,
            amount_raw=line.text,
            currency=currency,
            confidence=round(line.confidence or 0.0, 2),
            requires_review=(line.confidence or 0.0) < 0.80,
            ocr_lines=document.lines,
        )

    @staticmethod
    def _best_explicit_amount(lines: list[OcrLine]) -> tuple[OcrLine, float, str] | None:
        candidates: list[tuple[float, OcrLine, float, str]] = []
        for line in lines:
            match = MONEY_PATTERN.search(line.text)
            if not match or not TOTAL_LABEL_PATTERN.search(line.text):
                continue
            value = float(match.group(1).replace(",", "."))
            currency = "PEN" if match.group(0).upper().startswith(("S/", "PEN")) else "USD"
            candidates.append((line.confidence or 0.0, line, value, currency))
        return max(candidates, default=None, key=lambda item: item[0])[1:] if candidates else None
