use opencv::{
    core::{
        absdiff, bitwise_and, bitwise_not, bitwise_or, no_array, Mat, Point, Rect, Scalar,
        BORDER_CONSTANT, CV_8UC1,
    },
    highgui,
    imgcodecs::{self, IMREAD_GRAYSCALE},
    imgproc::{
        circle, dilate, erode, flood_fill, morphology_default_border_value, threshold, FILLED,
        THRESH_BINARY,
    },
    prelude::*,
    Result,
};

fn main() -> Result<()> {
    highgui::named_window("gears", 0)?;

    let image_file = imgcodecs::imread("./Gears.png", IMREAD_GRAYSCALE)?;

    let image_bin = {
        let mut binary = image_file.clone();
        threshold(&image_file, &mut binary, 100.0, 255.0, THRESH_BINARY)?;
        binary
    };

    highgui::imshow("gears", &image_bin)?;
    highgui::wait_key(-1)?;

    let image_filled = {
        let flooded = flooded_image(&image_bin, (0, 0), (255, 255, 255))?;
        or_image(&image_bin, &not_image(&flooded)?)?
    };

    highgui::imshow("gears", &image_filled)?;
    highgui::wait_key(-1)?;

    let image_diff = diff_image(&opening(&image_filled, 15)?, &image_filled)?;

    highgui::imshow("gears", &image_diff)?;
    highgui::wait_key(-1)?;

    let image_cleared_diff = closing(&image_diff, 3)?;

    highgui::imshow("gears", &image_cleared_diff)?;
    highgui::wait_key(-1)?;

    let image_ring = &dilated_image(&image_cleared_diff, 7, 2)?;

    highgui::imshow("gears", &image_ring)?;
    highgui::wait_key(-1)?;

    let image_cleared_ring = &opening(&image_ring, 7)?;

    highgui::imshow("gears", &image_cleared_ring)?;
    highgui::wait_key(-1)?;

    let dilated_diff = eroded_image(&dilated_image(&image_diff, 7, 3)?, 7)?;
    let break_points = dilated_image(
        &eroded_image(&diff_image(&image_cleared_ring, &dilated_diff)?, 7)?,
        15,
        3,
    )?;

    let result = or_image(&break_points, &image_cleared_ring)?;

    highgui::imshow("gears", &result)?;
    highgui::wait_key(-1)?;

    Ok(())
}

fn flooded_image(image: &Mat, seed: (i32, i32), color: (u8, u8, u8)) -> Result<Mat> {
    let mut clone = image.clone();
    let (w, h) = (image.cols(), image.rows());
    flood_fill(
        &mut clone,
        Point::new(seed.0, seed.1),
        Scalar::new(color.0 as f64, color.1 as f64, color.2 as f64, 255.0),
        &mut Rect::new(0, 0, w, h),
        Scalar::new(0.0, 0.0, 0.0, 0.0),
        Scalar::new(255.0, 255.0, 255.0, 255.0),
        4,
    )?;
    Ok(clone)
}

fn not_image(image: &Mat) -> Result<Mat> {
    let mut clone = image.clone();
    bitwise_not(&image, &mut clone, &no_array()?)?;
    Ok(clone)
}

fn or_image(left: &Mat, right: &Mat) -> Result<Mat> {
    let mut clone = left.clone();
    bitwise_or(&left, &right, &mut clone, &no_array()?)?;
    Ok(clone)
}

fn and_image(left: &Mat, right: &Mat) -> Result<Mat> {
    let mut clone = left.clone();
    bitwise_and(&left, &right, &mut clone, &no_array()?)?;
    Ok(clone)
}

fn opening(image: &Mat, size: usize) -> Result<Mat> {
    eroded_image(&dilated_image(image, size, 1)?, size)
}

fn closing(image: &Mat, size: usize) -> Result<Mat> {
    dilated_image(&eroded_image(image, size)?, size, 1)
}

fn dilated_image(image: &Mat, size: usize, times: usize) -> Result<Mat> {
    let mut clone = image.clone();
    let mut str_elem = Mat::zeros(size as i32, size as i32, CV_8UC1)?.to_mat()?;
    circle(
        &mut str_elem,
        Point::new(size as i32 / 2, size as i32 / 2),
        size as i32 / 2,
        Scalar::new(255.0, 255.0, 255.0, 255.0),
        0,
        FILLED,
        0,
    )?;
    let border = morphology_default_border_value()?;
    dilate(
        &image,
        &mut clone,
        &str_elem,
        Point::new(-1, -1),
        times as i32,
        BORDER_CONSTANT,
        border,
    )?;
    Ok(clone)
}

fn eroded_image(image: &Mat, size: usize) -> Result<Mat> {
    let mut clone = image.clone();
    let mut str_elem = Mat::zeros(size as i32, size as i32, CV_8UC1)?.to_mat()?;
    circle(
        &mut str_elem,
        Point::new(size as i32 / 2, size as i32 / 2),
        size as i32 / 2,
        Scalar::new(255.0, 255.0, 255.0, 255.0),
        0,
        FILLED,
        0,
    )?;
    let border = morphology_default_border_value()?;
    erode(
        &image,
        &mut clone,
        &str_elem,
        Point::new(-1, -1),
        1,
        BORDER_CONSTANT,
        border,
    )?;
    Ok(clone)
}

fn diff_image(left: &Mat, right: &Mat) -> Result<Mat> {
    let mut clone = left.clone();
    absdiff(&left, &right, &mut clone)?;
    Ok(clone)
}
