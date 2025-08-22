import logging
import sys
from datetime import datetime
from typing import Optional

def setup_logger(name: str, level: str = "INFO") -> logging.Logger:
    """Setup logger with consistent formatting"""
    
    # Create logger
    logger = logging.getLogger(name)
    logger.setLevel(getattr(logging, level.upper()))
    
    # Prevent duplicate handlers
    if logger.handlers:
        return logger
    
    # Create formatter
    formatter = logging.Formatter(
        fmt='%(asctime)s - %(name)s - %(levelname)s - %(message)s',
        datefmt='%Y-%m-%d %H:%M:%S'
    )
    
    # Create console handler
    console_handler = logging.StreamHandler(sys.stdout)
    console_handler.setLevel(logging.INFO)
    console_handler.setFormatter(formatter)
    
    # Add handlers to logger
    logger.addHandler(console_handler)
    
    return logger

def log_api_request(logger: logging.Logger, endpoint: str, user_id: Optional[str] = None, 
                   request_data: Optional[dict] = None):
    """Log API request with standardized format"""
    log_data = {
        'endpoint': endpoint,
        'user_id': user_id,
        'timestamp': datetime.utcnow().isoformat()
    }
    
    if request_data:
        log_data['request_size'] = len(str(request_data))
    
    logger.info(f"API Request: {log_data}")

def log_api_response(logger: logging.Logger, endpoint: str, status_code: int, 
                    response_time_ms: float, user_id: Optional[str] = None):
    """Log API response with standardized format"""
    log_data = {
        'endpoint': endpoint,
        'status_code': status_code,
        'response_time_ms': response_time_ms,
        'user_id': user_id,
        'timestamp': datetime.utcnow().isoformat()
    }
    
    logger.info(f"API Response: {log_data}")

def log_error(logger: logging.Logger, error: Exception, context: Optional[dict] = None):
    """Log error with context information"""
    error_data = {
        'error_type': type(error).__name__,
        'error_message': str(error),
        'timestamp': datetime.utcnow().isoformat()
    }
    
    if context:
        error_data['context'] = context
    
    logger.error(f"Error occurred: {error_data}", exc_info=True)
