from __future__ import annotations

import re

from app.models.ocr_result import OcrDocument, OcrLine, ReceiptAnalysis
from app.parsers.document_classifier import DocumentClassifier, DocumentType
from app.parsers.generic_receipt_parser import MONEY_PATTERN, GenericReceiptParser
from app.parsers.helpers.date_parser import parse_spanish_datetime

OPERATION_LABELS = ("código de operación", "codigo de operacion")
STOP_RECIPIENT_LABELS = ("comisión:", "comision:", "fecha y hora:", "código de operación:", "codigo de operacion:")
PHONE_DESTINATION_PATTERN = re.compile(
    r"\b(?P<phone>\d{3}\s?\d{3}\s?\d{3})\s*-\s*(?P<destination>yape|plin)\b",
    re.IGNORECASE,
)
OPERATION_PATTERN = re.compile(r"\b\d{6,}\b")


class PlinParser(GenericReceiptParser):
    def __init__(self, classifier: DocumentClassifier | None = None) -> None:
        self._classifier = classifier or DocumentClassifier()

    def matches(self, document: OcrDocument) -> bool:
        return self._classifier.detect(document) == DocumentType.PLIN

    def parse(self, document: OcrDocument) -> ReceiptAnalysis:
        amount = self._amount(document.lines)
        recipient = self._recipient(document.lines)
        phone, destination = self._phone_and_destination(document.lines)
        transaction_date = self._date(document.lines)
        operation_number = self._operation_number(document.lines)
        _, classifier_confidence = self._classifier.detect_with_confidence(document)

        confidence = self._confidence(
            classifier_confidence,
            amount[0].confidence if amount else None,
            recipient.confidence if recipient else None,
            transaction_date[0].confidence if transaction_date else None,
            operation_number[0].confidence if operation_number else None,
        )
        amount_line, amount_value = amount if amount else (None, None)
        operation_line, operation_value = operation_number if operation_number else (None, None)
        return ReceiptAnalysis(
            document_type="PLIN",
            amount=amount_value,
            amount_raw=amount_line.text if amount_line else None,
            currency="PEN" if amount_line else None,
            date=transaction_date[1] if transaction_date else None,
            recipient=recipient.text.strip() if recipient else None,
            recipient_phone=phone,
            destination=destination,
            operation_number=operation_value,
            confidence=confidence,
            requires_review=amount is None or operation_number is None or classifier_confidence < 0.70,
            ocr_lines=document.lines,
        )

    @staticmethod
    def _amount(lines: list[OcrLine]) -> tuple[OcrLine, float] | None:
        candidates: list[tuple[float, OcrLine, float]] = []
        for line in lines:
            match = MONEY_PATTERN.search(line.text)
            if match and match.group(0).upper().startswith(("S/", "PEN")):
                candidates.append((line.confidence or 0.0, line, float(match.group(1).replace(",", "."))))
        if not candidates:
            return None
        _, line, value = max(candidates, key=lambda item: item[0])
        return line, value

    @staticmethod
    def _recipient(lines: list[OcrLine]) -> OcrLine | None:
        for index, line in enumerate(lines):
            if line.text.strip().casefold() != "enviado a:":
                continue
            for candidate in lines[index + 1:]:
                normalized = candidate.text.strip().casefold()
                if normalized in STOP_RECIPIENT_LABELS:
                    return None
                if normalized:
                    return candidate
        return None

    @staticmethod
    def _phone_and_destination(lines: list[OcrLine]) -> tuple[str | None, str | None]:
        for line in lines:
            match = PHONE_DESTINATION_PATTERN.search(line.text)
            if match:
                return re.sub(r"\s+", "", match["phone"]), match["destination"].capitalize()
        return None, None

    @staticmethod
    def _date(lines: list[OcrLine]) -> tuple[OcrLine, str] | None:
        for line in lines:
            parsed = parse_spanish_datetime(line.text)
            if parsed is not None:
                return line, parsed.isoformat()
        return None

    @staticmethod
    def _operation_number(lines: list[OcrLine]) -> tuple[OcrLine, str] | None:
        for index, line in enumerate(lines[:-1]):
            if line.text.strip().casefold().rstrip(":") in OPERATION_LABELS:
                match = OPERATION_PATTERN.search(lines[index + 1].text)
                if match:
                    return lines[index + 1], match.group(0)
        return None

    @staticmethod
    def _confidence(*scores: float | None) -> float:
        available = [score for score in scores if score is not None]
        return round(min(sum(available) / len(available), 0.99), 2) if available else 0.0
